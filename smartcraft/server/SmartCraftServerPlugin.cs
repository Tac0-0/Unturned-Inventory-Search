// SmartCraft v1.4.1 experimental SERVER engine. Not deployed to production.
// Uses Unturned's own pre-craft event to substitute an in-memory blueprint.
// No new Workshop assets, client-side recipes, or replacement item IDs.
using System;
using System.Collections.Generic;
using System.Reflection;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using SDG.Unturned;

namespace SmartCraft.Server
{
    public sealed class SmartCraftServerPlugin : RocketPlugin
    {
        private const ushort Scrap = 67;
        private const ushort Scrap10 = 49101; // More Farming, "10 Metal Scraps"
        private const ushort Scrap100 = 49102; // More Farming, "100 Metal Scraps"
        private static readonly FieldInfo SuppliesField =
            typeof(Blueprint).GetField("_supplies", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo OutputsField =
            typeof(Blueprint).GetField("_outputs", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo SupplyItemRefField =
            typeof(BlueprintSupply).GetField("_itemRef", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly FieldInfo OutputItemRefField =
            typeof(BlueprintOutput).GetField("_itemRef", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo CloneMethod =
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        protected override void Load()
        {
            if (SuppliesField == null || OutputsField == null || CloneMethod == null ||
                SupplyItemRefField == null || OutputItemRefField == null)
            {
                Logger.LogError("[SmartCraft] Blueprint reflection layout changed; extension is disabled.");
                return;
            }
            PlayerCrafting.OnCraftBlueprintRequestedV2 += RewriteCraft;
            Logger.Log("[SmartCraft] Original-bundle crafting handler installed (experimental; cross-provider stack substitution + recipe cleanup).");
        }

        protected override void Unload()
        {
            PlayerCrafting.OnCraftBlueprintRequestedV2 -= RewriteCraft;
        }

        private static int Count(PlayerInventory inventory, ushort id)
        {
            int total = 0;
            for (byte page = PlayerInventory.SLOTS; page < PlayerInventory.PAGES; page++)
            {
                if (page == PlayerInventory.STORAGE || page == PlayerInventory.AREA) continue;
                byte count = inventory.getItemCount(page);
                for (byte index = 0; index < count; index++)
                {
                    ItemJar jar = inventory.getItem(page, index);
                    if (jar != null && jar.item != null && jar.item.id == id)
                        total++;
                }
            }
            return total;
        }

        private static bool TrySelectBundles(int required, int tensAvailable, int hundredsAvailable,
            out int tensUsed, out int hundredsUsed)
        {
            tensUsed = 0;
            hundredsUsed = 0;
            if (required <= 0) return true;
            bool found = false;
            long bestExcess = long.MaxValue;
            int bestItemCount = int.MaxValue;
            // Search bounded x100 choices and select the minimum required x10
            // for each. Minimize overpayment first, then bundle item count.
            // This avoids spending x100 when two x10 bundles suffice.
            int maxHundreds = Math.Min(hundredsAvailable, (required + 99) / 100);
            for (int hundreds = 0; hundreds <= maxHundreds; hundreds++)
            {
                int missing = Math.Max(0, required - hundreds * 100);
                int tens = (missing + 9) / 10;
                if (tens > tensAvailable) continue;
                long excess = 100L * hundreds + 10L * tens - required;
                int itemCount = tens + hundreds;
                if (!found || excess < bestExcess ||
                    (excess == bestExcess && itemCount < bestItemCount))
                {
                    found = true;
                    bestExcess = excess;
                    bestItemCount = itemCount;
                    tensUsed = tens;
                    hundredsUsed = hundreds;
                }
            }
            return found;
        }

        private static CachingBcAssetRef ResolveItemReference(ushort itemId)
        {
            ItemAsset asset = Assets.find(EAssetType.ITEM, itemId) as ItemAsset;
            if (asset == null || asset.id != itemId)
                throw new InvalidOperationException("Missing bundle asset ID " + itemId);
            // More Farming assigns GUIDs to its items. Bind the live asset,
            // not only its legacy number, so other recipe filters can resolve it.
            return new CachingBcAssetRef(asset);
        }

        private static BlueprintSupply CreateBundleSupply(ushort itemId, int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException("count");
            var supply = new BlueprintSupply(itemId, true, count, true,
                ECraftingInputPrioritization.LowestAmount);
            // IMPORTANT: BlueprintSupply(ushort...) does NOT initialize _itemRef.
            // Without this assignment server-side UpdateBlueprintDynamicStatus
            // marks the ingredient as missing, rejecting the craft.
            SupplyItemRefField.SetValue(supply, ResolveItemReference(itemId));
            if (supply.FindItemAsset() == null || supply.FindItemAsset().id != itemId)
                throw new InvalidOperationException("Cannot resolve new supply " + itemId);
            return supply;
        }

        private static BlueprintOutput CreateRefundOutput(ushort itemId, int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException("count");
            var output = new BlueprintOutput(itemId, count, EItemOrigin.CRAFT);
            // BlueprintOutput(ushort...) ALSO leaves _itemRef empty.
            OutputItemRefField.SetValue(output, ResolveItemReference(itemId));
            if (output.FindItemAsset() == null || output.FindItemAsset().id != itemId)
                throw new InvalidOperationException("Cannot resolve refund item " + itemId);
            return output;
        }

        private sealed class MaterialNeed
        {
            public ushort BaseId;
            public int Required;
        }

        private static Dictionary<ushort,int> Snapshot(PlayerInventory inventory)
        {
            var counts=new Dictionary<ushort,int>();
            for (byte page=PlayerInventory.SLOTS; page<PlayerInventory.PAGES; page++)
            {
                if (page==PlayerInventory.STORAGE || page==PlayerInventory.AREA)continue;
                byte count=inventory.getItemCount(page);
                for (byte index=0;index<count;index++)
                {
                    ItemJar jar=inventory.getItem(page,index);
                    if (jar==null || jar.item==null)continue;
                    ushort id=jar.item.id;int previous=0;
                    counts.TryGetValue(id,out previous);
                    counts[id]=previous+1;
                }
            }
            return counts;
        }

        private static int CountOf(Dictionary<ushort,int> inventory,ushort id)
        {
            int count=0;inventory.TryGetValue(id,out count);return count;
        }

        private static void ReturnChange(ushort baseId,int excess,List<BlueprintOutput> results)
        {
            if (excess<=0)return;
            // Prefer original More Farming compact x10/x100 where installed.
            // Native x12/x5 work for the families whose packs use them.
            foreach (int units in new int[]{100,12,10,5})
            {
                if (excess<units)continue;
                ushort bundle=SmartCraft.Shared.StackCatalog.PreferredRefund(baseId,units);
                if(bundle==0)continue;
                int amount=excess/units;
                results.Add(CreateRefundOutput(bundle,amount));
                excess-=amount*units;
            }
            if (excess>0)results.Add(CreateRefundOutput(baseId,excess));
        }

        private static void RewriteCraft(PlayerCrafting crafting,ref Blueprint blueprint,ref bool shouldAllow)
        {
            if (!shouldAllow || crafting==null || blueprint==null)return;
            if (blueprint.Operation!=EBlueprintOperation.None || blueprint.transferState)return;
            try
            {
                // A hidden recipe must also be blocked when someone submits its
                // crafting request directly rather than clicking in the Y menu.
                if (SmartCraft.Shared.RecipeCleanupPolicy.ShouldSuppressBlueprint(blueprint))
                { shouldAllow=false;return; }

                BlueprintSupply[] original=blueprint.supplies;
                if (original==null || original.Length<1)return;
                // Fail closed for stateful, critical non-consuming and quality-
                // dependent supplies until their preservation has been tested.
                var needs=new SortedDictionary<ushort,MaterialNeed>();
                var originalCounts=new Dictionary<ushort,int>();
                bool touchesStackFamily=false;
                foreach(BlueprintSupply supply in original)
                {
                    ItemAsset asset=supply==null ? null : supply.FindItemAsset();
                    if(asset==null || !supply.ShouldConsume ||
                        supply.CountingMethod!=ECraftingInputCountingMethod.TotalItems ||
                        supply.amount<=0)return;
                    ushort baseId;int units;
                    bool variant=SmartCraft.Shared.StackCatalog.TryMap(asset.id,out baseId,out units);
                    if(variant || SmartCraft.Shared.StackCatalog.ForBase(baseId)!=null)
                        touchesStackFamily=true;
                    MaterialNeed need;
                    if(!needs.TryGetValue(baseId,out need))
                    {
                        need=new MaterialNeed{BaseId=baseId,Required=0};
                        needs.Add(baseId,need);
                    }
                    need.Required=checked(need.Required+checked(supply.amount*units));
                    int previous=0;
                    originalCounts.TryGetValue(asset.id,out previous);
                    originalCounts[asset.id]=checked(previous+supply.amount);
                }
                if(!touchesStackFamily)return;
                PlayerInventory inv=crafting.player.inventory;
                Dictionary<ushort,int> counts=Snapshot(inv);
                bool originalsEnough=true;
                foreach(var item in originalCounts)
                    if(CountOf(counts,item.Key)<item.Value)
                    {originalsEnough=false;break;}
                if(originalsEnough)return; // preserve vanilla for normal crafting

                var supplyChanges=new List<BlueprintSupply>();
                var outputs=new List<BlueprintOutput>(blueprint.outputs);
                foreach(MaterialNeed need in needs.Values)
                {
                    var available=new List<SmartCraft.Shared.AvailableStack>();
                    int loose=CountOf(counts,need.BaseId);
                    if(loose>0 && SmartCraft.Shared.StackCatalog.IsAvailable(need.BaseId))
                        available.Add(new SmartCraft.Shared.AvailableStack(need.BaseId,1,loose));
                    List<SmartCraft.Shared.BundleVariant> variants=
                        SmartCraft.Shared.StackCatalog.ForBase(need.BaseId);
                    if(variants!=null)
                    {
                        foreach(SmartCraft.Shared.BundleVariant variant in variants)
                        {
                            int held=CountOf(counts,variant.Id);
                            if(held>0 && SmartCraft.Shared.StackCatalog.IsAvailable(variant.Id))
                                available.Add(new SmartCraft.Shared.AvailableStack(variant.Id,variant.Units,held));
                        }
                    }
                    SmartCraft.Shared.StackPlan plan;
                    if(!SmartCraft.Shared.StackPlanner.TryPlan(need.Required,available,out plan))
                        return; // native failure, no items touched
                    foreach(var chosen in plan.Selected)
                    {
                        supplyChanges.Add(CreateBundleSupply(chosen.Key,chosen.Value));
                    }
                    ReturnChange(need.BaseId,plan.Excess,outputs);
                }
                var replacement=(Blueprint)CloneMethod.Invoke(blueprint,null);
                SuppliesField.SetValue(replacement,supplyChanges.ToArray());
                OutputsField.SetValue(replacement,outputs.ToArray());
                int refundRows=outputs.Count-blueprint.outputs.Length;
                blueprint=replacement;
                Logger.Log("[SmartCraft] v1.4.1 cross-provider plan: "+needs.Count+
                    " material families; "+supplyChanges.Count+" physical input denominations; "+
                    refundRows+" refund outputs.");
            }
            catch(Exception ex)
            {
                shouldAllow=false;
                Logger.LogError("[SmartCraft] Refused unsafe cross-provider craft: "+ex);
            }
        }
    }
}
