// Tests Harmony can bind the patch signature without starting Unity or Unturned.
// Runtime game crafting still needs separate in-game verification.
using System;
using System.Reflection;
using HarmonyLib;
using SDG.Unturned;

public static class SmartCraftHarmonySignatureTest
{
    public static int Main(string[] argv)
    {
        if (argv.Length != 1) throw new Exception("Expected client DLL path");
        var original = typeof(PlayerCrafting).GetMethod("UpdateBlueprintDynamicStatus",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (original == null) throw new Exception("Crafting status method signature changed");
        var plugin = Assembly.LoadFrom(argv[0]);
        var patchType = plugin.GetType("SmartCraft.Client.BlueprintDynamicStatusPatch", true);
        var postfix = patchType.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
        if (postfix == null) throw new Exception("Harmony postfix missing");
        var paramTypes = original.GetParameters();
        if (paramTypes.Length != 1 ||
            !paramTypes[0].ParameterType.IsByRef ||
            paramTypes[0].ParameterType.GetElementType().FullName !=
                "SDG.Unturned.UpdateBlueprintStatusParameters")
            throw new Exception("Unturned crafting status ABI changed");

        var paramStatusType = typeof(PlayerCrafting).Assembly.GetType(
            "SDG.Unturned.UpdateBlueprintStatusParameters", true);
        var statusType = typeof(PlayerCrafting).Assembly.GetType(
            "SDG.Unturned.BlueprintStatus", true);
        var inputStatusType = typeof(PlayerCrafting).Assembly.GetType(
            "SDG.Unturned.BlueprintInputItemStatus", true);
        foreach (var name in new[] { "status" })
            if (paramStatusType.GetField(name) == null)
                throw new Exception("Missing parameter field " + name);
        foreach (var name in new[] { "blueprint", "inputItems",
            "totalMissingInputItemsCount", "isMissingAnyCriticalInputItem", "hasAnyInputItem" })
            if (statusType.GetField(name) == null)
                throw new Exception("Missing status field " + name);
        foreach (var name in new[] { "totalAmount", "isMissingRequiredAmount" })
            if (inputStatusType.GetField(name) == null)
                throw new Exception("Missing input field " + name);
        var visibility = typeof(PlayerCrafting).GetMethod("GatherUniqueInputItems",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (visibility == null || visibility.GetParameters().Length != 1)
            throw new Exception("Recipe visibility hook ABI changed");
        Console.WriteLine("PASS: Unturned Y-menu dynamic status and visibility ABI match alpha3");
        Console.WriteLine("NOTE: installing Harmony requires the live Unity engine; this is ABI-only verification.");
        return 0;
    }
}
