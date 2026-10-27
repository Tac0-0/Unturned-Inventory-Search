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
                var statusHook = AccessTools.Method(typeof(PlayerCrafting), "UpdateBlueprintDynamicStatus");
                var visibilityHook = AccessTools.Method(typeof(PlayerCrafting), "GatherUniqueInputItems");
                bool statusPatched = Harmony.GetPatchInfo(statusHook) != null;
                bool visibilityPatched = Harmony.GetPatchInfo(visibilityHook) != null;
                Logger.LogWarning("SmartCraft v1.3 alpha3 initialized: Y-menu visibility patched=" +
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
        private static int _lastReportedTens = -1, _lastReportedHundreds = -1;

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
            if (__instance == null || __args == null || __args.Length == 0 ||
                StatusParam == null || BlueprintField == null || InputItemsField == null ||
                MissingCountField == null || CriticalMissingField == null ||
                HasAnyInputField == null || TotalField == null || IsMissingField == null)
            {
                if (!_didLogFailure)
                {
                    _didLogFailure = true;
                    UnityEngine.Debug.LogError("[SmartCraft] v1.3-alpha3: unsupported Unturned crafting status reflection ABI");
                }
                return;
            }
            try
            {
                object status = StatusParam.GetValue(__args[0]);
                if (status == null) return;
                Blueprint bp = BlueprintField.GetValue(status) as Blueprint;
                if (bp == null || bp.Operation != EBlueprintOperation.None || bp.transferState) return;
                BlueprintSupply[] supplies = bp.supplies;
                IList inputStatuses = InputItemsField.GetValue(status) as IList;
                if (supplies == null || inputStatuses == null ||
                    inputStatuses.Count != supplies.Length) return;

                int scrapRows = 0;
                int rowIndex = -1;
                for (int i = 0; i < supplies.Length; i++)
                {
                    ItemAsset asset = supplies[i].FindItemAsset();
                    if (asset == null) continue;
                    if (asset.id == Scrap10 || asset.id == Scrap100) return;
                    if (asset.id != Scrap) continue;
                    // This stage supports original consumable Scrap materials,
                    // not alternative nonconsuming tools or special ammo amounts.
                    if (!supplies[i].ShouldConsume ||
                        supplies[i].CountingMethod != ECraftingInputCountingMethod.TotalItems) return;
                    scrapRows++;
                    rowIndex = i;
                }
                if (scrapRows != 1) return; // Avoid double allocation across duplicate supply rows.

                var inv = __instance.player.inventory;
                int tens = Count(inv, Scrap10);
                int hundreds = Count(inv, Scrap100);
                int bundleUnits = tens * 10 + hundreds * 100;
                if (bundleUnits <= 0) return;

                object row = inputStatuses[rowIndex];
                int loose = (int)TotalField.GetValue(row);
                int required = supplies[rowIndex].amount;
                int previousMissing = Math.Max(0, required - loose);
                int combined = checked(loose + bundleUnits);
                int currentMissing = Math.Max(0, required - combined);
                TotalField.SetValue(row, combined);
                IsMissingField.SetValue(row, currentMissing > 0);

                int previousTotalMissing = (int)MissingCountField.GetValue(status);
                MissingCountField.SetValue(status,
                    Math.Max(0, previousTotalMissing - previousMissing + currentMissing));
                HasAnyInputField.SetValue(status, true);

                bool criticalMissing = false;
                for (int i = 0; i < supplies.Length; i++)
                {
                    if (supplies[i].isCritical &&
                        (bool)IsMissingField.GetValue(inputStatuses[i]))
                    {
                        criticalMissing = true;
                        break;
                    }
                }
                CriticalMissingField.SetValue(status, criticalMissing);

                if (tens != _lastReportedTens || hundreds != _lastReportedHundreds)
                {
                    _lastReportedTens = tens;
                    _lastReportedHundreds = hundreds;
                    UnityEngine.Debug.LogWarning("[SmartCraft] v1.3-alpha3 Y-menu Scrap: " +
                        loose + " loose + " + tens + " x10 + " + hundreds +
                        " x100 = " + combined + " usable Scrap");
                }
            }
            catch (Exception ex)
            {
                if (!_didLogFailure)
                {
                    _didLogFailure = true;
                    UnityEngine.Debug.LogError("[SmartCraft] v1.3-alpha3 crafting status failure: " + ex);
                }
            }
        }
    }
}
