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
        var original = typeof(PlayerCrafting).GetMethod("UpdateBlueprintInputItemStatus",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (original == null) throw new Exception("Crafting status method signature changed");
        var plugin = Assembly.LoadFrom(argv[0]);
        var patchType = plugin.GetType("SmartCraft.Client.BlueprintInputQuantityPatch", true);
        var postfix = patchType.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
        if (postfix == null) throw new Exception("Harmony postfix missing");
        var patch = new Harmony("tac0.smartcraft.signature.test");
        patch.Patch(original, postfix: new HarmonyMethod(postfix));
        var info = Harmony.GetPatchInfo(original);
        if (info == null || info.Postfixes.Count < 1)
            throw new Exception("Harmony did not accept the private crafting method hook");
        patch.UnpatchSelf();
        Console.WriteLine("PASS: Harmony patch installed and removed for actual Unturned crafting method");
        return 0;
    }
}
