// Server-authoritative bounded change planner shared with offline tests.
// No Unity APIs: one inventory jar of a bundle contributes its unit count.
// A failed plan never modifies inventory or original blueprint definitions.
using System;
using System.Collections.Generic;

namespace SmartCraft.Shared
{
    public sealed class AvailableStack
    {
        public readonly ushort Id;
        public readonly int Units;
        public readonly int Count;
        public AvailableStack(ushort id,int units,int count)
        { Id=id;Units=units;Count=count; }
    }

    public sealed class StackPlan
    {
        public readonly SortedDictionary<ushort,int> Selected =
            new SortedDictionary<ushort,int>();
        public int Required;
        public int Total;
        public int Excess {get { return Total-Required; }}
    }

    public static class StackPlanner
    {
        private sealed class Node
        {
            public readonly ushort Id;
            public readonly int Quantity;
            public readonly Node Previous;
            public Node(ushort id,int quantity,Node previous)
            { Id=id;Quantity=quantity;Previous=previous; }
        }

        // Minimal OVERPAYMENT first, then number of consumed physical jars.
        // This is a bounded knapsack using binary-split quantities rather than
        // iterating hundreds of identical inventory jars.
        public static bool TryPlan(int required,IList<AvailableStack> available,out StackPlan plan)
        {
            plan=null;
            if (required<=0 || required>12000 || available==null) return false;
            int maxUnits=1;
            foreach (AvailableStack item in available)
            {
                if (item==null || item.Units<1 || item.Units>100 || item.Count<0)
                    return false;
                if (item.Count>0 && item.Units>maxUnits)maxUnits=item.Units;
            }
            int cap=checked(required+maxUnits-1);
            int[] costs=new int[cap+1];
            Node[] histories=new Node[cap+1];
            for (int i=1;i<=cap;i++)costs[i]=int.MaxValue;
            foreach (AvailableStack item in available)
            {
                if (item.Count==0) continue;
                int remaining=Math.Min(item.Count,cap/item.Units);
                int power=1;
                while (remaining>0)
                {
                    int take=Math.Min(power,remaining);
                    int units=take*item.Units;
                    for (int i=cap;i>=units;i--)
                    {
                        if (costs[i-units]==int.MaxValue)continue;
                        int candidate=costs[i-units]+take;
                        if (candidate<costs[i])
                        {
                            costs[i]=candidate;
                            histories[i]=new Node(item.Id,take,histories[i-units]);
                        }
                    }
                    remaining-=take;
                    if(power<16384)power*=2;
                }
            }
            int selected=-1;
            for (int i=required;i<=cap;i++)
                if (costs[i]!=int.MaxValue) {selected=i;break;}
            if (selected<0)return false;
            var built=new StackPlan {Required=required,Total=selected};
            for (Node n=histories[selected];n!=null;n=n.Previous)
            {
                int old=0;
                built.Selected.TryGetValue(n.Id,out old);
                built.Selected[n.Id]=old+n.Quantity;
            }
            int recount=0;
            foreach (AvailableStack item in available)
            {
                int selectedCount=0;
                built.Selected.TryGetValue(item.Id,out selectedCount);
                if (selectedCount>item.Count) return false;
                recount+=selectedCount*item.Units;
            }
            if (recount!=selected) return false;
            plan=built;
            return true;
        }
    }
}
