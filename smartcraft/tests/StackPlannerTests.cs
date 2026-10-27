using System;
using System.Collections.Generic;
using SmartCraft.Shared;

public static class StackPlannerTests
{
    private static int tests;
    private static void Expect(int wanted, int expectedExcess, params AvailableStack[] entries)
    {
        StackPlan plan;
        if(!StackPlanner.TryPlan(wanted,entries,out plan))
            throw new Exception("Planning failed for "+wanted);
        if(plan.Total!=wanted+expectedExcess)
            throw new Exception("Expected "+expectedExcess+" excess, got "+plan.Excess);
        tests++;
    }
    public static int Main()
    {
        Expect(4,0,new AvailableStack(67,1,11),new AvailableStack(49102,100,1));
        Expect(8,2,new AvailableStack(49101,10,1));
        Expect(18,2,new AvailableStack(49101,10,1),
            new AvailableStack(10162,10,1),new AvailableStack(1909,10,1));
        Expect(98,2,new AvailableStack(10163,100,1));
        Expect(9,3,new AvailableStack(1900,12,1));
        Expect(105,0,new AvailableStack(49102,100,1),
            new AvailableStack(1910,5,2));
        Expect(51,0,new AvailableStack(49101,10,5),
            new AvailableStack(67,1,1));
        Expect(3,0,new AvailableStack(67,1,3));
        StackPlan none;
        if(StackPlanner.TryPlan(22,new AvailableStack[]{new AvailableStack(49101,10,2)},out none))
            throw new Exception("Invented third bundle");
        tests++;
        if(StackPlanner.TryPlan(12001,new AvailableStack[]{new AvailableStack(49102,100,900)},out none))
            throw new Exception("Failed to bound planning");
        tests++;
        if(StackPlanner.TryPlan(0,new AvailableStack[]{new AvailableStack(67,1,1)},out none))
            throw new Exception("Zero-demand should not create items");
        tests++;
        // No plan may select more jars than physically available.
        for(int n=1;n<=175;n++)
        {
            var items=new AvailableStack[]{
                new AvailableStack(67,1,13),
                new AvailableStack(49101,10,4),
                new AvailableStack(10163,100,2)};
            StackPlan plan;
            bool ok=StackPlanner.TryPlan(n,items,out plan);
            if(!ok)throw new Exception("Cannot cover "+n);
            int sum=0;
            foreach(var item in items)
            {
                int count=0;plan.Selected.TryGetValue(item.Id,out count);
                if(count>item.Count)throw new Exception("Overspent items");
                sum+=item.Units*count;
            }
            if(sum!=plan.Total || sum<n || plan.Excess>=100)
                throw new Exception("Unit balance violation "+n);
            tests++;
        }
        Console.WriteLine("PASS "+tests+" bounded-knapsack and cross-provider transaction tests");
        return 0;
    }
}
