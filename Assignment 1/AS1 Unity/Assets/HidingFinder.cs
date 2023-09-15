using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class HidingFinder : MonoBehaviour
{
    public float RayCount = 10f;
    //private Quaternion rayStep { get => Quaternion.Euler(0, 360 / RayCount, 0); }// Quaternion.AngleAxis(360 / RayCount, transform.forward); }

    public List<RaycastHit> Rays = new List<RaycastHit>();
    public Vector3[] Surrounding_Hits = new Vector3[0];
    public Vector3[] Opposing_Hits = new Vector3[0];
    public LinkedNodes[] ClosestNodes = new LinkedNodes[0];


    WaypointGraph graphNodes;
    GameObject activePlayer;
    public void Start()
    {
        graphNodes = GameObject.FindGameObjectWithTag("waypoint graph").GetComponent<WaypointGraph>();
        activePlayer = GameObject.FindGameObjectWithTag("Player");
    }



    public void performTest()
    {
        if(graphNodes == null)
            graphNodes = GameObject.FindGameObjectWithTag("waypoint graph").GetComponent<WaypointGraph>();

        if(activePlayer == null)
            activePlayer = GameObject.FindGameObjectWithTag("Player");

        Rays.Clear();
        Surrounding_Hits = new Vector3[(int)RayCount];
        Opposing_Hits = new Vector3[(int)RayCount];
        ClosestNodes = new LinkedNodes[(int)RayCount];

        RaycastHit hit;
        Vector3 dir = transform.forward;
        

        for(int x = 0; x < RayCount; x++)
        {
            if (Physics.Raycast(transform.position, dir, out hit))
            {
                Rays.Add(hit);
                Surrounding_Hits[x] = hit.point;

                Debug.DrawRay(hit.point, -dir, Color.cyan, 25f);
                hit.collider.Raycast(new Ray(hit.point + (dir.normalized * 50), -dir), out hit, 50);
                Opposing_Hits[x] = hit.point;
            }

            dir = Quaternion.AngleAxis(360f / RayCount, Vector3.up) * dir;
        }

        for(int i = 0; i < RayCount; i++)
        {
            ClosestNodes[i] = findClosestViableWaypoint(Opposing_Hits[i]);
        }
    }

    //Find the waypoint that is the closest to where we have clicked the mouse
    private LinkedNodes findClosestViableWaypoint(Vector3 point)
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
            if (!Physics.Linecast(activePlayer.transform.position, graphNodes.graphNodes[i].transform.position))//Linecast = true if collider between the points
                continue;

            minDistance = dist;
            WaypointIndex = i;
        }
        return graphNodes.graphNodes[WaypointIndex].GetComponent<LinkedNodes>();
    }


    void OnDrawGizmos()
    {
        Handles.color = Gizmos.color;
        for (int i = 0; i < RayCount; i++)
        {
            //Surrounding - In view points
            Gizmos.color = new Color(255f, 165f, 0f);
            Handles.color = Gizmos.color;
            Gizmos.DrawSphere(Surrounding_Hits[i], 0.25f);
            //Handles.Label(Surrounding_Hits[i], Surrounding_Hits[i].ToString());

            //Opposing - Out of view points (other side of objects)
            Gizmos.color = Color.green;
            Handles.color = Gizmos.color;
            Gizmos.DrawSphere(Opposing_Hits[i], 0.25f);
            //Handles.Label(Opposing_Hits[i], Opposing_Hits[i].ToString());
        }
    }
}

[CustomEditor(typeof(HidingFinder))]
public class HidingInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        HidingFinder script = (HidingFinder)target;
        
        if(GUILayout.Button("Run Test Search"))
            script.performTest();
    }
}