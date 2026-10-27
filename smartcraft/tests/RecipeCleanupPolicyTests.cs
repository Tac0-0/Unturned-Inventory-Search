using System;
using SmartCraft.Shared;

internal static class RecipeCleanupPolicyTests
{
    private static int _tests;
    private static void Check(bool shouldRemove, ushort[] inputs, int[] amounts,
        ushort[] outputs, int[] outputAmounts, string why, bool ordinary = true,
        bool[] consumed = null)
    {
        if (consumed == null)
        {
            consumed = new bool[inputs.Length];
            for (int i = 0; i < inputs.Length; i++) consumed[i] = true;
        }
        bool actual = RecipeCleanupPolicy.ShouldSuppressNumeric(
            inputs, amounts, consumed, outputs, outputAmounts, ordinary);
        if (actual != shouldRemove)
            throw new Exception(why + ": expected " + shouldRemove + " but got " + actual);
        _tests++;
    }

    public static int Main()
    {
        // Can recycle loses one Scrap relative to the original crafting recipe.
        Check(true, new ushort[]{72},new int[]{1},new ushort[]{67},new int[]{3},"Can salvage");
        Check(false,new ushort[]{67},new int[]{4},new ushort[]{72},new int[]{1},"Make Can for 4");
        Check(false,new ushort[]{72},new int[]{1},new ushort[]{67},new int[]{4},
            "Do not suppress a hypothetical 4-Scrap Can salvage");

        // Same product, same quantity, different storage footprints.
        Check(true,new ushort[]{67},new int[]{10},new ushort[]{10162},new int[]{1},
            "More Stacking 2x2 Scrap x10");
        Check(true,new ushort[]{67},new int[]{10},new ushort[]{1909},new int[]{1},
            "vanilla 2x2 Scrap x10");
        Check(false,new ushort[]{67},new int[]{10},new ushort[]{49101},new int[]{1},
            "More Farming 1x1 Scrap x10");
        Check(true,new ushort[]{49101},new int[]{10},new ushort[]{10163},new int[]{1},
            "More Stacking 3x3 Scrap x100 from More Farming x10");
        Check(false,new ushort[]{49101},new int[]{10},new ushort[]{49102},new int[]{1},
            "Prefer 2x2 Scrap x100");
        Check(true,new ushort[]{66},new int[]{10},new ushort[]{10178},new int[]{1},
            "Cloth 2x2 x10");
        Check(false,new ushort[]{66},new int[]{10},new ushort[]{49103},new int[]{1},
            "Cloth 1x1 x10");

        // Never remove paths to rescue existing inferior bundles.
        Check(false,new ushort[]{10162},new int[]{1},new ushort[]{67},new int[]{10},
            "Unstack inferior Scrap x10");
        Check(false,new ushort[]{10163},new int[]{1},new ushort[]{10162},new int[]{10},
            "Unstack inferior Scrap x100");
        Check(false,new ushort[]{1909},new int[]{1},new ushort[]{67},new int[]{10},
            "Vanilla unstack");
        Check(false,new ushort[]{49101},new int[]{1},new ushort[]{67},new int[]{10},
            "More Farming unstack");

        // Fail closed rather than accidentally hiding unrelated/complex recipes.
        Check(false,new ushort[]{67},new int[]{9},new ushort[]{10162},new int[]{1},
            "Nonconserving inputs");
        Check(false,new ushort[]{67,66},new int[]{10,1},new ushort[]{10162},new int[]{1},
            "Unrelated second ingredient");
        Check(false,new ushort[]{67},new int[]{10},new ushort[]{10162,66},
            new int[]{1,1},"Recipe includes a byproduct");
        Check(false,new ushort[]{67},new int[]{10},new ushort[]{10162},new int[]{1},
            "Stateful or repair crafting",false);
        Check(false,new ushort[]{67},new int[]{10},new ushort[]{10162},new int[]{1},
            "Non-consumable ingredient",true,new bool[]{false});
        Check(false,new ushort[]{67},new int[]{10},new ushort[]{10163},new int[]{1},
            "Different denomination size");
        Check(false,new ushort[]{68},new int[]{5},new ushort[]{10162},new int[]{1},
            "Wrong base material");
        Check(false,new ushort[]{72},new int[]{1},new ushort[]{67,66},new int[]{3,1},
            "Can with byproducts remains for manual review");
        Console.WriteLine("PASS: " + _tests +
            " recipe suppression regression cases, including compact stacking, Can and unstacking");
        return 0;
    }
}
