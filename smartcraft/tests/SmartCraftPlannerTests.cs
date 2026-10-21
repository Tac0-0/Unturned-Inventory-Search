// Compile alongside the server prototype; invokes the production planner itself.
// No running Unturned instance or Rocket server required.
using System;
using System.Reflection;
using SmartCraft.Server;

public static class SmartCraftPlannerTests
{
    private static int assertions;
    private static readonly MethodInfo Planner = typeof(SmartCraftServerPlugin)
        .GetMethod("TrySelectBundles", BindingFlags.NonPublic | BindingFlags.Static);

    private static void Case(int cost, int available10, int available100,
        bool expectedPossible, int expected10, int expected100)
    {
        var args = new object[] { cost, available10, available100, 0, 0 };
        bool possible = (bool)Planner.Invoke(null, args);
        int tens = (int)args[3], hundreds = (int)args[4];
        if (possible != expectedPossible || tens != expected10 || hundreds != expected100)
        {
            throw new Exception("PLAN FAILED cost=" + cost + " available 10=" +
                available10 + " 100=" + available100 + " => " + possible +
                ",10x=" + tens + ",100x=" + hundreds);
        }
        if (possible && (10 * tens + 100 * hundreds < cost)) throw new Exception("Underpaid!");
        assertions++;
    }

    public static int Main()
    {
        if (Planner == null) throw new Exception("Planner missing");
        Case(2, 0, 1, true, 0, 1);    // x100 => 9 x10 and 8 loose change
        Case(2, 1, 0, true, 1, 0);    // x10 => 8 loose change
        Case(2, 1, 1, true, 1, 0);    // prefer x10 over x100
        Case(19, 2, 1, true, 2, 0);   // never spend x100 unnecessarily
        Case(21, 3, 1, true, 3, 0);
        Case(21, 1, 1, true, 0, 1);
        Case(100, 10, 1, true, 0, 1); // both exact; fewer bundle objects
        Case(110, 1, 1, true, 1, 1);
        Case(101, 0, 2, true, 0, 2);
        Case(10, 0, 0, false, 0, 0);
        Case(101, 0, 1, false, 0, 0);
        Case(0, 0, 0, true, 0, 0);
        Console.WriteLine("PASS: " + assertions + " production bundle-planner scenarios");
        return 0;
    }
}
