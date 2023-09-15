using System;
using System.Collections.Generic;
using System.Linq;

public class FibHeap<T>
{
    public int Count { get => Count_; }
    private int Count_ = 0;
    public int RootCount => recount(minNode, onlyRoots: true);
    FibHeapNode<T> minNode = null;

    #region Externals
    /// <summary>
    /// Handles pushing a new node into the heap
    /// </summary>
    /// <param name="N_node">The node to add</param>
    /// <param name="consoling">Used to identify if the push operation is being conducted through Consolidate, only to avoid accidental count modification</param>
    public void Push(FibHeapNode<T> N_node, bool consoling = false)
    {
        if (Count_ <= 0 || minNode == null)
        {
            minNode = N_node;
            if (!consoling)
                Count_++;
            return;
        }


        //Replace Min with new node
        //Step rightand place Min node back in

        if (minNode.L != null)
        {
            minNode.L.R = N_node;
            N_node.L = minNode.L;
        }

        N_node.R = minNode;
        minNode.L = N_node;

        if (N_node < minNode)
            minNode = N_node;

        if (!consoling)
            Count_++;
    }

    /// <summary>
    /// Pops the node with the lowest value out of the heap
    /// </summary>
    /// <returns>The lowest valued node</returns>
    public FibHeapNode<T> Pop()
    {
        if (Count_ <= 0 || minNode == null)
            return null;

        // - Add children to root list
        // - Remove the minNode
        // - Push the minNode pointer to the right
        // - Consolidate

        //Step 1
        FibHeapNode<T> lastNode = PushChildren(minNode.Children.ToArray());
        minNode.Children.Clear();

        FibHeapNode<T> temp = minNode;

        if (lastNode == null)
        {
            if(minNode.L != null)
                minNode.L.R = minNode.R;

            if (minNode.R != null)
                minNode.R.L = minNode.L;

            if (minNode.L != null)
            {
                minNode = minNode.L;
            }
            else if(minNode.R != null) 
            {
                minNode = minNode.R;
            }
            else
            {
                minNode = null;
            }
        }
        else
        {
            //Step 2

            lastNode.R = minNode.R;
            if (minNode.R != null) 
            {
                minNode.R.L = lastNode;
            }

            //Step 3
            minNode = lastNode;
        }

        temp.L = null;
        temp.R = null;
        Count_--;

        //Step 4
        Consolidate();
        return temp;
    }

    public void Clear()
    {
        minNode = null;
        Count_ = 0;
    }


    public bool Contains(T target, Func<T, T, bool> Comparison)
    {
        return containsSearch(minNode, target, Comparison, true);
    }

    #endregion

    #region Internals
    readonly static double DegreePhi = 1.0 / Math.Log((1 + Math.Sqrt(5)) / 2);
    void Consolidate()
    {
        if (Count_ <= 0 || minNode == null)
            return;

        // For each node in the root tree:
        //  - Store the node relational to its degree
        //  - If there is already a node of a given degree stored
        //  - Merge the trees and step it to the next degree
        //  - Repeat till the tree can no longer step up/steps into an empty degree


        //Max Degrees is logN technically, but was having issues with the degree of accuracy 
        FibHeapNode<T>[] Degrees = new FibHeapNode<T>[(int)Math.Floor((Math.Log(Count_) * DegreePhi) + 1)];

        //Go to the absolute left of the tree to start (not needed, probably not helpful either)
        FibHeapNode<T> stepping = (FibHeapNode<T>)minNode.Clone();
        while (stepping.L != null)
            stepping = stepping.L;

        FibHeapNode<T> Target = (FibHeapNode<T>)stepping.Clone();
        while (true)
        {
            if (Degrees[Target.Degrees] != null)
            {
                int index = Target.Degrees;
                FibHeapNode<T> Contained = Degrees[index];
                
                if (Contained < Target)
                {
                    Target.L = Target.R = null;
                    Contained.Children.Add(Target);
                    Target = Contained;
                    Degrees[index] = null;
                    continue;
                }
                else
                {
                    Contained.L = Contained.R = null;
                    Target.Children.Add(Contained);
                    Degrees[index] = null;
                    continue;
                }
            }
            else
            {
                Degrees[Target.Degrees] = (FibHeapNode<T>)Target.Clone();
            }

            if (stepping.R == null)
                break;

            Target = (FibHeapNode<T>)stepping.R.Clone();
            stepping = stepping.R;
        }

        minNode = null;
        foreach (FibHeapNode<T> fheap_n in Degrees)
        {
            if (fheap_n != null)
            {
                fheap_n.L = fheap_n.R = null;
                Push(fheap_n, consoling: true);
            }
        }
    }

    //HAS NO RECURSION/REPEAT PROTECTION
    //Shouldn't need it tho as consolidating removes references to L & R when merging, and re-inserting
    private bool containsSearch(FibHeapNode<T> cur, T target, Func<T, T, bool> Comparison, bool firstTime = true)
    {
        //count is invalid since count could be zero while minNode is null
        if (minNode == null)
            return false;

        if (Comparison(cur.Association, target))
            return true;


        //Step all the way left
        if (firstTime)
        {
            cur = minNode;
            while (cur.L != null)
                cur = cur.L;
        }


        if (cur.Children.Count > 0)
        {
            foreach (FibHeapNode<T> n in cur.Children)
            {
                bool found = containsSearch(n, target, Comparison, false);
                if (found)
                    return true;
            }
        }

        if (cur.R != null)
        {
            bool found = containsSearch(cur.R, target, Comparison, false);
            if (found)
                return true;
        }

        return false;
    }

    FibHeapNode<T> PushChildren(FibHeapNode<T>[] l)
    {
        FibHeapNode<T> last = null;
        for (int index = 0; index < l.Count(); index++)
        {
            last = l[index];
            Push(l[index], true);
        }
            
        return last;
    }

    public void ForceCountUpdate() => Count_ = recount();

    //For recounting when adding entire trees instead of just 1 node for testing
    //only for dev
    //HAS NO RECURSION/REPEAT PROTECTION
    int recount(FibHeapNode<T> cur = null, bool firstTime = true, bool onlyRoots = false, bool isChild = false)
    {
        //count is invalid since count could be zero while minNode is null
        if (minNode == null)
            return 0;

        int count = 1;

        //Step all the way left
        if (firstTime)
        {
            cur = minNode;
            while (cur.L != null)
                cur = cur.L;
        }


        if (cur.Children.Count > 0 && !onlyRoots)
        {
            foreach (FibHeapNode<T> n in cur.Children)
            {
                //Consolidating only resets L R on the root nodes, not the children processed 
                //n.L = n.R = null;
                count += recount(n, false, isChild: true);
            }
        }

        if (cur.R != null && !isChild)
        {
            count += recount(cur.R, false);
        }

        return count;
    }
    #endregion
}

public class FibHeapNode<T> : ICloneable
{
    public FibHeapNode(T Association)
    {
        this.Association = Association;
    }

    public FibHeapNode<T> Parent = null;
    public FibHeapNode<T> L = null;
    public FibHeapNode<T> R = null;
    public List<FibHeapNode<T>> Children = new List<FibHeapNode<T>>();
    public int Degrees => Children.Count;

    public double Value;
    public T Association;

    public static bool operator <(FibHeapNode<T> lhs, FibHeapNode<T> rhs) => lhs.Value < rhs.Value;
    public static bool operator >(FibHeapNode<T> lhs, FibHeapNode<T> rhs) => lhs.Value > rhs.Value;

    public override string ToString() => $"{(L == null ? "__" : L.Value.ToString())} <- {Value} -> {(R == null ? "__" : R.Value.ToString())}";

    public object Clone()
    {
        return new FibHeapNode<T>(this.Association)
        {
            Children = this.Children,
            L = this.L,
            R = this.R,
            Parent = this.Parent,
            Value = this.Value,
        };
    }
}
