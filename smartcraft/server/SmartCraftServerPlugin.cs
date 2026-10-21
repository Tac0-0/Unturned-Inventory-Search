// SmartCraft v1.3 experimental SERVER engine. Not deployed to production.
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
        private static readonly MethodInfo CloneMethod =
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        protected override void Load()
        {
            if (SuppliesField == null || OutputsField == null || CloneMethod == null)
            {
                Logger.LogError("[SmartCraft] Blueprint reflection layout changed; extension is disabled.");
                return;
            }
            PlayerCrafting.OnCraftBlueprintRequestedV2 += RewriteCraft;
            Logger.Log("[SmartCraft] Original-bundle crafting handler installed (experimental, scrap-only).");
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

        private static void RewriteCraft(PlayerCrafting crafting, ref Blueprint blueprint, ref bool shouldAllow)
        {
            if (!shouldAllow || crafting == null || blueprint == null) return;
            // State-transfer/repair/fill operations require a separate preservation strategy.
            if (blueprint.Operation != EBlueprintOperation.None || blueprint.transferState) return;
            try
            {
                BlueprintSupply[] originals = blueprint.supplies;
                if (originals == null || originals.Length == 0) return;

                // Never intercept recipes that themselves explicitly use a bundle ID.
                foreach (BlueprintSupply s in originals)
                    if ((s.FindItemAsset() != null && (s.FindItemAsset().id == Scrap10 || s.FindItemAsset().id == Scrap100))) return;

                PlayerInventory inventory = crafting.player.inventory;
                int loose = Count(inventory, Scrap);
                int tens = Count(inventory, Scrap10);
                int hundreds = Count(inventory, Scrap100);
                if (tens + hundreds == 0) return;

                int totalScrapRequired = 0;
                foreach (BlueprintSupply s in originals)
                {
                    if ((s.FindItemAsset() == null || s.FindItemAsset().id != Scrap)) continue;
                    if (!s.ShouldConsume || s.CountingMethod != ECraftingInputCountingMethod.TotalItems)
                        return;
                    totalScrapRequired += s.amount;
                }
                if (totalScrapRequired <= loose || totalScrapRequired < 1) return;
                int neededFromBundles = totalScrapRequired - loose;
                int tensUsed, hundredsUsed;
                if (!TrySelectBundles(neededFromBundles, tens, hundreds, out tensUsed, out hundredsUsed))
                    return; // Vanilla fails the request without consuming anything.

                // Do not mutate asset-owned Blueprint or BlueprintSupply instances.
                var inputs = new List<BlueprintSupply>();
                int looseRemaining = loose;
                foreach (BlueprintSupply original in originals)
                {
                    if ((original.FindItemAsset() == null || original.FindItemAsset().id != Scrap)) { inputs.Add(original); continue; }
                    int thisLoose = Math.Min(original.amount, looseRemaining);
                    looseRemaining -= thisLoose;
                    if (thisLoose > 0)
                    {
                        var copy = (BlueprintSupply)CloneMethod.Invoke(original, null);
                        copy.amount = thisLoose;
                        inputs.Add(copy);
                    }
                }
                if (tensUsed > 0)
                    inputs.Add(new BlueprintSupply(Scrap10, true, tensUsed, false,
                        ECraftingInputPrioritization.LowestAmount));
                if (hundredsUsed > 0)
                    inputs.Add(new BlueprintSupply(Scrap100, true, hundredsUsed, false,
                        ECraftingInputPrioritization.LowestAmount));

                int excess = loose + 10 * tensUsed + 100 * hundredsUsed - totalScrapRequired;
                if (excess < 0) { shouldAllow = false; return; }
                var outputs = new List<BlueprintOutput>(blueprint.outputs);
                // Do not create ammo-style items. Refund exact CHANGE using real
                // More Farming x10 bundles plus original loose scrap objects.
                int returnedTens = excess / 10;
                int returnedLoose = excess % 10;
                if (returnedTens > 0)
                    outputs.Add(new BlueprintOutput(Scrap10, returnedTens, EItemOrigin.CRAFT));
                if (returnedLoose > 0)
                    outputs.Add(new BlueprintOutput(Scrap, returnedLoose, EItemOrigin.CRAFT));

                var replacement = (Blueprint)CloneMethod.Invoke(blueprint, null);
                SuppliesField.SetValue(replacement, inputs.ToArray());
                OutputsField.SetValue(replacement, outputs.ToArray());
                blueprint = replacement; // Vanilla still validates the recipe and gives output.
                Logger.Log("[SmartCraft] Server blueprint rewrite: " + totalScrapRequired +
                    " scrap, consuming " + loose + " loose + " + tensUsed + " x10 + " +
                    hundredsUsed + " x100; returning " + returnedTens + " x10 + " +
                    returnedLoose + " loose.");
            }
            catch (Exception e)
            {
                // A plugin failure must never grant outputs or debit only part of an input.
                shouldAllow = false;
                Logger.LogError("[SmartCraft] Refused unsafe craft: " + e);
            }
        }
    }
}
