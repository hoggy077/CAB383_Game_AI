using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


#region Implementation
public class NavigationAgent : MonoBehaviour {

    //Navigation Variables
    public WaypointGraph graphNodes;

    
    public FibHeap<LinkedNodes> openHeap = new FibHeap<LinkedNodes>();      //A* only       | Leaving these as int since comparing by index is more valid then by node
    public List<int> closedList = new List<int>();                          //A* & Greedy   | While Paths and other info is handled by node rather than index


    public List<Vector3> currentPath = new List<Vector3>();             //Changed       | Converted to Vector3 is it allows for more distinct debugging in the scene rather than this number business
    public List<LinkedNodes> PathingResults = new List<LinkedNodes>();  //Added         | Current path is the complete path, this is a buffer for a set of results. Greedy returns if the node was accessible,
                                                                        //              | so this allows storage of that path without tampering with current path on a failed result.
    //public List<int> greedyPaintList = new List<int>();               //Deprecated    | Dropped greedy paint list as closedList functions the same in both contexts, and is cleared before running either making it more managable

    public int currentNodeIndex = 0;
    public LinkedNodes currentNode { get => graphNodes.graphNodes[currentNodeIndex].GetComponent<LinkedNodes>(); }








    void Start () {
        graphNodes = GameObject.FindGameObjectWithTag("waypoint graph").GetComponent<WaypointGraph>();

        PathingResults.Add(currentNode);                    //Added     | Pathing results is the path as raw LinkedNodes and loses 0 at the same time as current path
        currentPath.Add(currentNode.transform.position);    //Changed   | Changed to use currentNode
    }





    //A-Star Search - Swap to node, cut this int id crap
    public bool AStarSearch(LinkedNodes start, LinkedNodes goal, out List<LinkedNodes> Path) {
        
        Reset();
        start.G = 0;
        start.H = Heuristic(start, goal);
        openHeap.Push(new FibHeapNode<LinkedNodes>(start)
        {
            Value = start.F
        });


        LinkedNodes Result = start;

        while(openHeap.Count > 0)
        {

            FibHeapNode<LinkedNodes> min = openHeap.Pop();
            closedList.Add(min.Association.index);

            if (min.Association == goal)
            {
                Result = min.Association;
                break;
            }

            for(int i = 0; i < min.Association.NodeSiblings.Length; i++)
            {
                if (closedList.Contains(min.Association.NodeSiblings[i].index))
                    continue;


                float NewG = min.Association.G + Heuristic(min.Association, min.Association.NodeSiblings[i]);
                bool isContained = openHeap.Contains(min.Association.NodeSiblings[i], FibHeapCompare);
                if (NewG < min.Association.NodeSiblings[i].G || !isContained){
                    min.Association.NodeSiblings[i].G = NewG;
                    min.Association.NodeSiblings[i].H = Heuristic(goal.transform.position, min.Association.NodeSiblings[i].transform.position);
                    min.Association.NodeSiblings[i].AStar_Parent = min.Association;

                    if (!isContained)
                        openHeap.Push(new FibHeapNode<LinkedNodes>(min.Association.NodeSiblings[i])
                        {
                            Value = min.Association.NodeSiblings[i].F
                        });
                }
            }
        }

        //Convert to list of steps
        //If check skips extra bigO should the point be unreachable
        if(Result == start)
            Path = new List<LinkedNodes>();
        else 
            Path = retracePath(start, Result);
        return Result != start;
    }

    // Reasoning    | FibHeap stores the nodes but has no direct method of equating the difference in value aside from "Value" which is used for sorting
    //              | This combined with the fact I made it as a generic class for future use outside of this assignment this provides a good level of 
    //              | adaptability with any future contents
    private bool FibHeapCompare(LinkedNodes HostValue, LinkedNodes OtherValue) => HostValue == OtherValue;

    private List<LinkedNodes> retracePath(LinkedNodes start, LinkedNodes Astar_Result)
    {
        List<LinkedNodes> path = new List<LinkedNodes>();
        LinkedNodes currentNode = Astar_Result;
        while (true)
        {
            if (currentNode == start)
                break;
            path.Add(currentNode);
            currentNode = currentNode.AStar_Parent;
        }
        path.Reverse();
        return path;
    }



    
    //Greedy Search
    public bool GreedySearch(LinkedNodes start, LinkedNodes goal, out List<LinkedNodes> FinalPath) 
    {
        //Reasoning  | Returning a bool to validate the node is accessible & outputting a final path of LinkedNodes means more things can be achieved
        //           | Although, it does mean we're restricted to working in 1 context without simplifying to node ID's
        //           | It does open up the door to active pathing through a moving node 

        Reset();

        Stack<LinkedNodes> Path = new Stack<LinkedNodes>();
        Path.Push(start);
        closedList.Add(start.index);

        bool canReach = Greedy(start, goal, ref Path, ref closedList);
        FinalPath = Stack2List(Path);

        return canReach;
    }

    private bool Greedy(LinkedNodes current, LinkedNodes goal, ref Stack<LinkedNodes> Active_Path, ref List<int> Closures)
    {
        if (!Active_Path.Contains(current)) //Start is added prior so this is just to skip it
            Active_Path.Push(current);

        if(current.index == goal.index)
            return true;


        //Get Children, sort by Heuristic
        List<LinkedNodes> siblings = new List<LinkedNodes>();
        foreach(LinkedNodes s in current.NodeSiblings)
        {
            s.H = Heuristic(s, goal);
            siblings.Add(s);
        }
        siblings.Sort(); //-- double check this is the right order


        for(int siblingIndex = 0; siblingIndex < siblings.Count; siblingIndex++)
        {
            //If the node is already in the path, or closed
            if (Closures.Contains(siblings[siblingIndex].index) || Active_Path.Contains(siblings[siblingIndex]))
                continue; //skip it

            //Check next sibling
            bool result = Greedy(siblings[siblingIndex], goal, ref Active_Path, ref Closures);
            if(result)
                return true; //Path goal was reached, so

            //Otherwise, we checked this path and got no where
            //Step back on the path, and close the node we just finished
            Closures.Add(Active_Path.Pop().index);
        }
        return false;
    }




    #region Utils
    void Reset()
    {
        currentPath.Clear();
        openHeap.Clear();
        closedList.Clear();
        currentPath.Clear();
        PathingResults.Clear();
    }

    //Reverse Greedy
    List<LinkedNodes> Stack2List(Stack<LinkedNodes> pathStack)
    {
        LinkedNodes[] res = new LinkedNodes[pathStack.Count - 1];
        for (int i = pathStack.Count-1; i > 0; i--)
            res[i - 1] = pathStack.Pop();
        return res.ToList();
    }

    public List<Vector3> Path2Vector(IEnumerable<LinkedNodes> pathStack)
    {
        List<Vector3> result = new List<Vector3>(pathStack.Count());
        foreach (LinkedNodes node in pathStack)
        {
            result.Add(new Vector3(node.transform.position.x, transform.position.y, node.transform.position.z));
        }
            
        return result;
    }





    public float Heuristic(int NodeA, int NodeB) =>
        Vector3.Distance(
            graphNodes.graphNodes[NodeA].transform.position,
            graphNodes.graphNodes[NodeB].transform.position
            );

    static public float Heuristic(Vector3 NodeA, Vector3 NodeB) =>
        Vector3.Distance(
            NodeA,
            NodeB
            );

    static public float Heuristic(LinkedNodes NodeA, LinkedNodes NodeB) =>
        Vector3.Distance(
            NodeA.transform.position,
            NodeB.transform.position
            );
    #endregion
}


#endregion

