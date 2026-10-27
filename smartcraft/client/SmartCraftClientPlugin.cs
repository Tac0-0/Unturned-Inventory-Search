// SmartCraft v1.3 experimental CLIENT addon; BepInEx 5 + Harmony 2.
// Never changes item IDs, recipe definitions, or artwork.
// Requires a compatible SmartCraft server. Not part of stable v1.2 releases.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
                var statusHook = AccessTools.Method(typeof(PlayerCrafting), "UpdateBlueprintInputItemStatus");
                var visibilityHook = AccessTools.Method(typeof(PlayerCrafting), "GatherUniqueInputItems");
                bool statusPatched = Harmony.GetPatchInfo(statusHook) != null;
                bool visibilityPatched = Harmony.GetPatchInfo(visibilityHook) != null;
                Logger.LogInfo("SmartCraft v1.3 alpha2: Y-menu visibility patched=" +
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
        private static readonly ItemAsset ScrapAsset =
            Assets.find(EAssetType.ITEM, (ushort)67) as ItemAsset;

        private static void Postfix(HashSet<ItemAsset> results)
        {
            if (results == null) return;
            ItemAsset baseAsset = ScrapAsset ??
                Assets.find(EAssetType.ITEM, (ushort)67) as ItemAsset;
            if (baseAsset == null) return;
            bool hasBundle = false;
            foreach (ItemAsset asset in results)
            {
                if (asset != null && (asset.id == 49101 || asset.id == 49102))
                {
                    hasBundle = true;
                    break;
                }
            }
            if (hasBundle) results.Add(baseAsset);
        }
    }

    [HarmonyPatch(typeof(PlayerCrafting), "UpdateBlueprintInputItemStatus")]
    internal static class BlueprintInputQuantityPatch
    {
        private const ushort Scrap = 67;
        private const ushort Scrap10 = 49101;
        private const ushort Scrap100 = 49102;
        private static bool _didLogFailure;

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

        // Unturned deliberately marks its status classes INTERNAL. Harmony's
        // __args is the safe ABI here, while cached FieldInfo restricts access
        // to just the quantities and missing flags we need.
        private static readonly Type ParametersType =
            typeof(PlayerCrafting).Assembly.GetType("SDG.Unturned.UpdateBlueprintStatusParameters");
        private static readonly FieldInfo ParamStatus =
            ParametersType == null ? null :
            AccessTools.Field(ParametersType, "status");
        private static readonly Type StatusType =
            typeof(PlayerCrafting).Assembly.GetType("SDG.Unturned.BlueprintStatus");
        private static readonly FieldInfo StatusBlueprint =
            StatusType == null ? null : AccessTools.Field(StatusType, "blueprint");
        private static readonly FieldInfo MissingCount =
            StatusType == null ? null : AccessTools.Field(StatusType, "totalMissingInputItemsCount");
        private static readonly FieldInfo CriticalMissing =
            StatusType == null ? null : AccessTools.Field(StatusType, "isMissingAnyCriticalInputItem");
        private static readonly FieldInfo HasAnyInput =
            StatusType == null ? null : AccessTools.Field(StatusType, "hasAnyInputItem");
        private static readonly FieldInfo InputStatuses =
            StatusType == null ? null : AccessTools.Field(StatusType, "inputItems");
        private static readonly Type InputStatusType =
            typeof(PlayerCrafting).Assembly.GetType("SDG.Unturned.BlueprintInputItemStatus");
        private static readonly FieldInfo InputTotal =
            InputStatusType == null ? null : AccessTools.Field(InputStatusType, "totalAmount");
        private static readonly FieldInfo InputMissing =
            InputStatusType == null ? null : AccessTools.Field(InputStatusType, "isMissingRequiredAmount");

        private static void Postfix(PlayerCrafting __instance, object[] __args, ref bool __result)
        {
            if (__instance == null || __args == null || __args.Length < 3 ||
                ParamStatus == null || StatusBlueprint == null || MissingCount == null ||
                CriticalMissing == null || HasAnyInput == null || InputStatuses == null ||
                InputTotal == null || InputMissing == null) return;
            BlueprintSupply inputConfig = __args[1] as BlueprintSupply;
            object inputStatus = __args[2];
            if (inputConfig == null || inputStatus == null || !inputConfig.ShouldConsume ||
                inputConfig.CountingMethod != ECraftingInputCountingMethod.TotalItems) return;
            ItemAsset asset = inputConfig.FindItemAsset();
            if (asset == null || asset.id != Scrap) return;

            try
            {
                object status = ParamStatus.GetValue(__args[0]);
                if (status == null) return;
                Blueprint blueprint = StatusBlueprint.GetValue(status) as Blueprint;
                if (blueprint == null || blueprint.Operation != EBlueprintOperation.None ||
                    blueprint.transferState) return;
                foreach (BlueprintSupply supply in blueprint.supplies)
                    if ((supply.FindItemAsset() != null && (supply.FindItemAsset().id == Scrap10 || supply.FindItemAsset().id == Scrap100))) return;

                PlayerInventory inventory = __instance.player.inventory;
                int bundleUnits = 10 * Count(inventory, Scrap10) +
                                  100 * Count(inventory, Scrap100);
                if (bundleUnits <= 0) return;
                int before = (int)InputTotal.GetValue(inputStatus);
                int required = inputConfig.amount;
                int missingBefore = Math.Max(0, required - before);
                int newAmount = before + bundleUnits;
                int missingAfter = Math.Max(0, required - newAmount);
                InputTotal.SetValue(inputStatus, newAmount);
                InputMissing.SetValue(inputStatus, missingAfter > 0);
                int currentMissing = (int)MissingCount.GetValue(status);
                MissingCount.SetValue(status, Math.Max(0, currentMissing - missingBefore + missingAfter));
                HasAnyInput.SetValue(status, true);

                // Keep critical flags set for OTHER missing recipe ingredients.
                IList items = InputStatuses.GetValue(status) as IList;
                bool anyMissingCritical = false;
                if (items != null)
                {
                    int n = Math.Min(items.Count, blueprint.supplies.Length);
                    for (int i = 0; i < n; i++)
                    {
                        if (blueprint.supplies[i].isCritical &&
                            (bool)InputMissing.GetValue(items[i]))
                        {
                            anyMissingCritical = true;
                            break;
                        }
                    }
                }
                CriticalMissing.SetValue(status, anyMissingCritical);
                if (missingAfter == 0) __result = false;
            }
            catch (Exception ex)
            {
                // Do not swallow first critical error: a user-facing BepInEx log
                // is essential to diagnose changes in Unturned's client runtime.
                if (!_didLogFailure)
                {
                    _didLogFailure = true;
                    UnityEngine.Debug.LogError("[SmartCraft] Failed to calculate Y-menu quantities: " + ex);
                }
            }
        }
    }
}
