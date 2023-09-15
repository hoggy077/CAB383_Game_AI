using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Enemy : NavigationAgent {

    //Player Reference
    public Player playerRef;

    //Movement Variables
    public float moveSpeed = 10.0f;
    public float rotateDuration = 10.0f;
    public float minDistance = 0.1f;

    public GameObject Indicator;


    // Use this for initialization
    void Start() {
        //Find waypoint graph
        graphNodes = GameObject.FindGameObjectWithTag("waypoint graph").GetComponent<WaypointGraph>();
        //Initial node index to move to
        currentPath.Add(currentNode.transform.position);
        PathingResults.Add(currentNode);
        //Establish reference to player game object
        playerRef = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

        FSM_Setup();
    }

    public FSMv2 Manager = new FSMv2();
    public virtual void FSM_Setup() { }



    public void setIndicator(Color32 color) =>
        Indicator.GetComponent<MeshRenderer>().material.color = color;



    #region Movement
    //Move Enemy
    public void Move() {
        if (currentPath.Count > 0) {

            if (!LookAtPoint(currentPath[0]))
                return;

            //Move Towards next point
            transform.position = Vector3.MoveTowards(transform.position, currentPath[0], moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, currentPath[0]) <= minDistance)
            {
                currentNodeIndex = PathingResults[0].index;
                currentPath.RemoveAt(0);
                PathingResults.RemoveAt(0);
                DirSet = false; //Reset the look towards for the next loop
            }
        }
    }


    //Rotate towards the point
    //Quaternion targetDir;
    //bool DirSet = false;
    //float LerpTime = 0;
    //public bool LookAtPoint(Vector3 point)
    //{
    //    //This is just convenience so LerpTime isn't reset constantly & we dont recalculate targetDir
    //    if (!DirSet)
    //    {
    //        targetDir = //Quaternion.LookRotation((point - currentNode.transform.position).normalized);
    //        LerpTime = 0;
    //        DirSet = true;
    //    }


    //    LerpTime += Time.deltaTime;
    //    if ( targetDir != transform.rotation && Quaternion.Inverse(targetDir) != transform.rotation)//&& LerpTime <= rotateDuration)
    //    {
    //        transform.rotation = Quaternion.Lerp(transform.rotation, targetDir, Mathf.Clamp(LerpTime, 0, rotateDuration) / rotateDuration);    //Added | Using Lerp correctly for once is weird
    //        return false;
    //    }

    //    return true;
    //}

    Quaternion targetDir = new Quaternion();
    bool DirSet = false;
    float LerpTime = 0;

    public void resetLookState()
    {
        LerpTime = 0;
        DirSet = false;
    }

    public bool LookAtPoint(Vector3 point)
    {
        //This is just convenience so LerpTime isn't reset constantly & we dont recalculate targetDir
        if (!DirSet)
        {
            targetDir = Quaternion.LookRotation((point - transform.position).normalized);
            LerpTime = 0;
            DirSet = true;
        }


        LerpTime += Time.deltaTime;
        if (Quaternion.Angle(transform.rotation, targetDir) > 0.00000001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetDir, Mathf.Clamp(LerpTime, 0, rotateDuration) / rotateDuration);    //Added | Using Lerp correctly for once is weird
            return false;
        }

        return true;
    }
    #endregion

    #region Util
    public LinkedNodes findClosestViableWaypoint(Vector3 point)
    {
        int WaypointIndex = 0;
        float minDistance = Mathf.Infinity;
        for (int i = 0; i < graphNodes.graphNodes.Length; i++)
        {
            //Dist check
            float dist = Vector3.Distance(point, graphNodes.graphNodes[i].transform.position);
            if (dist > minDistance)
                continue;

            //LOS check | Player to Node
            if (!Physics.Linecast(playerRef.transform.position, graphNodes.graphNodes[i].transform.position))//Linecast = true if collider between the points
                continue;

            minDistance = dist;
            WaypointIndex = i;
        }
        return graphNodes.graphNodes[WaypointIndex].GetComponent<LinkedNodes>();
    }

    public  LinkedNodes findClosestWaypoint(Vector3 point)
    {
        int WaypointIndex = 0;
        float minDistance = Mathf.Infinity;
        for (int i = 0; i < graphNodes.graphNodes.Length; i++)
        {
            //Dist check
            float dist = Vector3.Distance(point, graphNodes.graphNodes[i].transform.position);
            if (dist > minDistance)
                continue;

            minDistance = dist;
            WaypointIndex = i;
        }
        return graphNodes.graphNodes[WaypointIndex].GetComponent<LinkedNodes>();
    }
    #endregion
}

