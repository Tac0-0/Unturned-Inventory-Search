// SmartCraft v1.4.1 experimental CLIENT addon; BepInEx 5 + Harmony 2.
// Never changes item IDs, recipe definitions, or artwork.
// Requires a compatible SmartCraft server. Cross-mod stack equivalence is experimental and requires the matching server.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using SDG.Unturned;

namespace SmartCraft.Client
{
    [BepInPlugin("tac0.smartcraft.client", "SmartCraft Crafting Add-on", "1.4.1")]
    [BepInDependency("tac0.inventorysearch", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class SmartCraftClientPlugin : BaseUnityPlugin
    {
        private Harmony _harmony;
        // Unlike the MonoBehaviour host, this log source survives scene loading.
        internal static readonly BepInEx.Logging.ManualLogSource HookLog =
            BepInEx.Logging.Logger.CreateLogSource("SmartCraft Hooks");

        private void Awake()
        {
            try
            {
                _harmony = new Harmony("tac0.smartcraft.client.1.4");
                _harmony.PatchAll(typeof(SmartCraftClientPlugin).Assembly);
                var statusHook = AccessTools.Method(typeof(PlayerCrafting), "UpdateBlueprintDynamicStatus");
                var visibilityHook = AccessTools.Method(typeof(PlayerCrafting), "GatherUniqueInputItems");
                bool statusPatched = Harmony.GetPatchInfo(statusHook) != null;
                bool visibilityPatched = Harmony.GetPatchInfo(visibilityHook) != null;
                Logger.LogWarning("SmartCraft v1.4.1 test initialized: Y-menu visibility patched=" +
                    visibilityPatched + ", bundle count patched=" + statusPatched);
            }
            catch (Exception ex)
            {
                Logger.LogError("SmartCraft disabled because the crafting patch failed: " + ex);
                if (_harmony != null) _harmony.UnpatchSelf();
            }
        }

        private void OnDestroy()
        {
            // Unturned destroys BepInEx's shared plugin host at frame 0 while
            // Application.isPlaying remains true (confirmed by Inventory
            // Search's frame-0 logs). Removing the Harmony patches here made
            // SmartCraft inert after a successful "patched=True" startup.
            if (UnityEngine.Application.isPlaying)
            {
                HookLog.LogWarning("Host destroyed during gameplay; keeping Harmony patches active. frame=" +
                    UnityEngine.Time.frameCount);
                return;
            }
            HookLog.LogInfo("Game shutting down; unpatching SmartCraft.");
            if (_harmony != null) _harmony.UnpatchSelf();
        }
    }

    // In the native Y menu the craftable-only filter runs BEFORE the quantity
    // checks. It builds a HashSet of literal inventory ItemAsset IDs. When the
    // player carries only 49101/49102 the vanilla Scrap 67 entry is filtered
    // out entirely, so the quantity postfix is never reached.
    //
    // Add virtual base Scrap availability to the temporary UI search set,
    // not to the inventory or a Blueprint. This keeps the ORIGINAL recipes.
    [HarmonyPatch(typeof(PlayerCrafting), "GatherUniqueInputItems")]
    internal static class CraftingMenuVisibilityPatch
    {
        private static bool _reportedOnce;

        private static void Postfix(HashSet<ItemAsset> results)
        {
            if (!_reportedOnce)
            {
                _reportedOnce=true;
                SmartCraftClientPlugin.HookLog.LogInfo("v1.4.1 Y menu cross-provider visibility hook active.");
            }
            if (results==null || results.Count==0)return;
            var families=new HashSet<ushort>();
            foreach(ItemAsset asset in results)
            {
                if(asset==null)continue;
                ushort baseId;int units;
                bool variant=SmartCraft.Shared.StackCatalog.TryMap(asset.id,out baseId,out units);
                if(variant || SmartCraft.Shared.StackCatalog.ForBase(baseId)!=null)
                    families.Add(baseId);
            }
            foreach(ushort baseId in families)
            {
                ItemAsset original=Assets.find(EAssetType.ITEM,baseId) as ItemAsset;
                if(original==null)continue;
                results.Add(original);
                List<SmartCraft.Shared.BundleVariant> variants=SmartCraft.Shared.StackCatalog.ForBase(baseId);
                if(variants==null)continue;
                foreach(SmartCraft.Shared.BundleVariant variant in variants)
                {
                    if(!SmartCraft.Shared.StackCatalog.IsAvailable(variant.Id))continue;
                    ItemAsset bundle=Assets.find(EAssetType.ITEM,variant.Id) as ItemAsset;
                    if(bundle!=null)results.Add(bundle);
                }
            }
        }
    }

    // The actual recipe card uses BlueprintInputItemStatus.totalAmount,
    // calculated during UpdateBlueprintDynamicStatus. Patch the completion
    // of that entire calculation, not an individual intermediate item search.
    // The original v1.3-alpha1/2 patch could silently fail to update the
    // displayed amount: it inspected an internal struct mid-calculation.
    [HarmonyPatch(typeof(PlayerCrafting), "UpdateBlueprintDynamicStatus")]
    internal static class BlueprintDynamicStatusPatch
    {
        private const ushort Scrap = 67;
        private const ushort Scrap10 = 49101;
        private const ushort Scrap100 = 49102;
        private static bool _didLogFailure;
        private static bool _loggedInvocation;

        private static readonly Type ParamsType =
            typeof(PlayerCrafting).Assembly.GetType("SDG.Unturned.UpdateBlueprintStatusParameters");
        private static readonly FieldInfo StatusParam =
            ParamsType == null ? null : AccessTools.Field(ParamsType, "status");
        private static readonly Type StatusType =
            typeof(PlayerCrafting).Assembly.GetType("SDG.Unturned.BlueprintStatus");
        private static readonly FieldInfo BlueprintField =
            StatusType == null ? null : AccessTools.Field(StatusType, "blueprint");
        private static readonly FieldInfo InputItemsField =
            StatusType == null ? null : AccessTools.Field(StatusType, "inputItems");
        private static readonly FieldInfo MissingCountField =
            StatusType == null ? null : AccessTools.Field(StatusType, "totalMissingInputItemsCount");
        private static readonly FieldInfo CriticalMissingField =
            StatusType == null ? null : AccessTools.Field(StatusType, "isMissingAnyCriticalInputItem");
        private static readonly FieldInfo HasAnyInputField =
            StatusType == null ? null : AccessTools.Field(StatusType, "hasAnyInputItem");
        private static readonly Type InputStatusType =
            typeof(PlayerCrafting).Assembly.GetType("SDG.Unturned.BlueprintInputItemStatus");
        private static readonly FieldInfo TotalField =
            InputStatusType == null ? null : AccessTools.Field(InputStatusType, "totalAmount");
        private static readonly FieldInfo IsMissingField =
            InputStatusType == null ? null : AccessTools.Field(InputStatusType, "isMissingRequiredAmount");

        private static int Count(PlayerInventory inventory, ushort id)
        {
            int total = 0;
            // Match Unturned's own GatherUniqueInputItems inventory pages.
            // STORAGE/AREA represent external/world items, not held materials.
            for (byte page = 0; page < PlayerInventory.STORAGE; page++)
            {
                byte itemCount = inventory.getItemCount(page);
                for (byte index = 0; index < itemCount; index++)
                {
                    ItemJar jar = inventory.getItem(page, index);
                    if (jar != null && jar.item != null && jar.item.id == id)
                        total++;
                }
            }
            return total;
        }

        private static void Postfix(PlayerCrafting __instance, object[] __args)
        {
            if (!_loggedInvocation)
            {
                _loggedInvocation=true;
                SmartCraftClientPlugin.HookLog.LogInfo("v1.4.1 Y menu cross-provider status hook active.");
            }
            if (__instance==null || __args==null || __args.Length==0 ||
                StatusParam==null || BlueprintField==null || InputItemsField==null ||
                MissingCountField==null || CriticalMissingField==null ||
                HasAnyInputField==null || TotalField==null || IsMissingField==null)
            {
                if(!_didLogFailure)
                {
                    _didLogFailure=true;
                    SmartCraftClientPlugin.HookLog.LogError("v1.4.1 unsupported Unturned crafting status ABI.");
                }
                return;
            }
            try
            {
                object status=StatusParam.GetValue(__args[0]);
                if(status==null)return;
                Blueprint bp=BlueprintField.GetValue(status) as Blueprint;
                if(bp==null || bp.Operation!=EBlueprintOperation.None || bp.transferState)return;
                BlueprintSupply[] supplies=bp.supplies;
                IList inputStatuses=InputItemsField.GetValue(status) as IList;
                if(supplies==null || inputStatuses==null || supplies.Length!=inputStatuses.Count)return;
                // Use the same fail-closed eligibility gate as the server.
                // Do not partially rewrite the status before discovering an
                // unsupported supply in a later recipe row.
                foreach(BlueprintSupply supply in supplies)
                {
                    ItemAsset asset=supply==null ? null : supply.FindItemAsset();
                    if(asset==null || !supply.ShouldConsume ||
                        supply.CountingMethod!=ECraftingInputCountingMethod.TotalItems ||
                        supply.amount<=0)return;
                }
                var inventory=new Dictionary<ushort,int>();
                PlayerInventory inv=__instance.player.inventory;
                for(byte page=PlayerInventory.SLOTS;page<PlayerInventory.PAGES;page++)
                {
                    if(page==PlayerInventory.STORAGE || page==PlayerInventory.AREA)continue;
                    byte count=inv.getItemCount(page);
                    for(byte index=0;index<count;index++)
                    {
                        ItemJar jar=inv.getItem(page,index);
                        if(jar==null||jar.item==null)continue;
                        ushort id=jar.item.id;int old=0;
                        inventory.TryGetValue(id,out old);
                        inventory[id]=old+1;
                    }
                }
                var availableBase=new Dictionary<ushort,int>();
                foreach(var item in inventory)
                {
                    ushort baseId;int units;
                    bool variant=SmartCraft.Shared.StackCatalog.TryMap(item.Key,out baseId,out units);
                    if(!variant && SmartCraft.Shared.StackCatalog.ForBase(baseId)==null)continue;
                    if(!SmartCraft.Shared.StackCatalog.IsAvailable(item.Key))continue;
                    int current=0;availableBase.TryGetValue(baseId,out current);
                    availableBase[baseId]=checked(current+item.Value*units);
                }
                int totalMissing=(int)MissingCountField.GetValue(status);
                bool hasAny=(bool)HasAnyInputField.GetValue(status);
                bool changed=false;
                for(int i=0;i<supplies.Length;i++)
                {
                    BlueprintSupply supply=supplies[i];
                    ItemAsset asset=supply==null ? null:supply.FindItemAsset();
                    if(asset==null)continue;
                    ushort baseId;int units;
                    bool isVariant=SmartCraft.Shared.StackCatalog.TryMap(asset.id,out baseId,out units);
                    if(!isVariant && SmartCraft.Shared.StackCatalog.ForBase(baseId)==null)continue;
                    if(!supply.ShouldConsume ||
                       supply.CountingMethod!=ECraftingInputCountingMethod.TotalItems ||
                       supply.amount<=0) return; // preserve native stateful semantics
                    int remaining=0;availableBase.TryGetValue(baseId,out remaining);
                    int virtualItems=Math.Max(0,remaining/units);
                    object row=inputStatuses[i];
                    int vanillaAmount=(int)TotalField.GetValue(row);
                    int vanillaMissing=Math.Max(0,supply.amount-vanillaAmount);
                    int equivalentMissing=Math.Max(0,supply.amount-virtualItems);
                    TotalField.SetValue(row,virtualItems);
                    IsMissingField.SetValue(row,equivalentMissing>0);
                    totalMissing=Math.Max(0,totalMissing-vanillaMissing+equivalentMissing);
                    hasAny=hasAny||virtualItems>0;
                    int reserved=checked(Math.Min(remaining,checked(supply.amount*units)));
                    availableBase[baseId]=remaining-reserved;
                    changed=true;
                }
                if(!changed)return;
                MissingCountField.SetValue(status,totalMissing);
                HasAnyInputField.SetValue(status,hasAny);
                bool criticalMissing=false;
                for(int i=0;i<supplies.Length;i++)
                    if(supplies[i].isCritical &&
                       (bool)IsMissingField.GetValue(inputStatuses[i]))
                    {criticalMissing=true;break;}
                CriticalMissingField.SetValue(status,criticalMissing);
            }
            catch(Exception ex)
            {
                if(!_didLogFailure)
                {
                    _didLogFailure=true;
                    SmartCraftClientPlugin.HookLog.LogError("v1.4.1 cross-provider status failure: "+ex);
                }
            }
        }
    }
    // v1.4 DEV: hide inferior production recipes consistently with the
    // Rocket server's authoritative rejection. Original assets remain intact.
    [HarmonyPatch(typeof(PlayerCrafting), "IsBlueprintPermanentlyDisabled")]
    internal static class RecipeCleanupVisibilityPatch
    {
        private static void Postfix(Blueprint blueprint, ref bool __result)
        {
            if (__result) return;
            if (SmartCraft.Shared.RecipeCleanupPolicy.ShouldSuppressBlueprint(blueprint))
                __result = true;
        }
    }

}
