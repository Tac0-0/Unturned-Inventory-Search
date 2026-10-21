// SmartCraft v1.3 experimental CLIENT addon; BepInEx 5 + Harmony 2.
// Never changes item IDs, recipe definitions, or artwork.
// Requires a compatible SmartCraft server. Not part of stable v1.2 releases.
using System;
using BepInEx;
using HarmonyLib;
using SDG.Unturned;

namespace SmartCraft.Client
{
    [BepInPlugin("tac0.smartcraft.client", "SmartCraft Crafting Add-on", "1.3.0")]
    [BepInDependency("tac0.inventorysearch", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class SmartCraftClientPlugin : BaseUnityPlugin
    {
        private Harmony _harmony;

        private void Awake()
        {
            try
            {
                _harmony = new Harmony("tac0.smartcraft.client.1.3");
                _harmony.PatchAll(typeof(SmartCraftClientPlugin).Assembly);
                Logger.LogInfo("SmartCraft 1.3: bundle-aware vanilla Y crafting status patch loaded.");
            }
            catch (Exception ex)
            {
                Logger.LogError("SmartCraft disabled because the crafting patch failed: " + ex);
                if (_harmony != null) _harmony.UnpatchSelf();
            }
        }

        private void OnDestroy()
        {
            if (_harmony != null) _harmony.UnpatchSelf();
        }
    }

    [HarmonyPatch(typeof(PlayerCrafting), "UpdateBlueprintInputItemStatus")]
    internal static class BlueprintInputQuantityPatch
    {
        private const ushort Scrap = 67;
        private const ushort Scrap10 = 49101;
        private const ushort Scrap100 = 49102;

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

        // p is the SAME UpdateBlueprintStatusParameters struct used by vanilla
        // crafting. Postfix adjusts status after the game's normal search.
        // Critical failure status is recalculated from genuine input rows.
        private static void Postfix(PlayerCrafting __instance,
            ref UpdateBlueprintStatusParameters p,
            BlueprintSupply inputItemConfig,
            BlueprintInputItemStatus inputItemStatus,
            ItemJar itemToIgnore,
            ref bool __result)
        {
            if (__instance == null || p.status == null || inputItemConfig == null ||
                inputItemStatus == null) return;
            if (!inputItemConfig.ShouldConsume ||
                inputItemConfig.CountingMethod != ECraftingInputCountingMethod.TotalItems)
                return;
            ItemAsset asset = inputItemConfig.FindItemAsset();
            if (asset == null || asset.id != Scrap) return;
            Blueprint blueprint = p.status.blueprint;
            if (blueprint == null || blueprint.Operation != EBlueprintOperation.None ||
                blueprint.transferState) return;
            foreach (BlueprintSupply s in blueprint.supplies)
                if (s.id == Scrap10 || s.id == Scrap100) return;

            try
            {
                PlayerInventory inventory = __instance.player.inventory;
                int bundleUnits = 10 * Count(inventory, Scrap10) +
                                  100 * Count(inventory, Scrap100);
                if (bundleUnits <= 0) return;
                int before = inputItemStatus.totalAmount;
                int required = inputItemConfig.amount;
                int missingBefore = Math.Max(0, required - before);
                int newAmount = before + bundleUnits;
                int missingAfter = Math.Max(0, required - newAmount);
                inputItemStatus.totalAmount = newAmount;
                inputItemStatus.isMissingRequiredAmount = missingAfter > 0;
                p.status.totalMissingInputItemsCount =
                    Math.Max(0, p.status.totalMissingInputItemsCount - missingBefore + missingAfter);
                p.status.hasAnyInputItem = true;

                // Preserve other missing critical inputs (e.g., an Alicepack).
                bool anyMissingCritical = false;
                int count = Math.Min(p.status.inputItems.Count, blueprint.supplies.Length);
                for (int i = 0; i < count; i++)
                {
                    if (blueprint.supplies[i].isCritical &&
                        p.status.inputItems[i].isMissingRequiredAmount)
                    {
                        anyMissingCritical = true;
                        break;
                    }
                }
                p.status.isMissingAnyCriticalInputItem = anyMissingCritical;

                // Vanilla returns true to stop scanning remaining inputs if the
                // inventory had no item of the exact ID. Continue when bundles
                // satisfy this input.
                if (missingAfter == 0) __result = false;
            }
            catch (Exception)
            {
                // Leave vanilla result unchanged in case of API incompatibility.
            }
        }
    }
}
