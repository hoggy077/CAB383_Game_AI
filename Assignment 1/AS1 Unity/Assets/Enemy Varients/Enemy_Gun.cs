using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Enemy_Gun : Enemy
{
    //                      Variables
    //
    //[ Variable Name       | Type      | Content            ]
    //[---------------------|-----------|--------------------]
    //[ Player_dist         | Float     | Distance to player ]
    //[ Player_los          | Bool      | LOS to player      ]
    //[ Enemy_moving        | bool      | Is roaming         ]
    //[ Enemy_main          | Enemy_Gun | Script Reference   ]
    //[ Player_main         | Player    | Script Reference   ]


    //                  FSM (NFA)
    //
    //[ State   | Transition Condition  | New State ]
    //[---------|-----------------------|-----------]
    //[ Roam    | LOS + Distance > ply  | Attack    ]
    //[ Roam    | LOS + Distance < ply  | Run       ]
    //[ Attack  | Distance < ply + LOS  | Run       ]
    //[ Run     | Distance > ply + Time | Roam      ]

    public GameObject Bullet;
    public Transform Bullet_Spawn;

    public override void FSM_Setup()
    {
        Manager.updateVariable("Player_dist", Mathf.Infinity);
        Manager.updateVariable("Player_los", false);
        Manager.updateVariable("Enemy_moving", false);
        Manager.updateVariable("Enemy_main", this);
        Manager.updateVariable("Player_main", playerRef);
        Manager.updateVariable("Enemy_bullet", Bullet);
        Manager.updateVariable("Enemy_bullet_spawn", Bullet_Spawn);

        Manager.setDefaultState("Enemy_roam");
        Manager.addState(new Enemy_Gun_Roam(Manager)); //Roam
        Manager.addState(new Enemy_Gun_Attack(Manager)); //Attack
        Manager.addState(new Enemy_Gun_Run(Manager)); //Run
    }

    public void Update()
    {
        RaycastHit hit;
        Physics.Linecast(transform.position, playerRef.transform.position, out hit);
        Manager.updateVariables(
            new KeyValuePair<string, object>("Player_dist", Vector3.Distance(playerRef.transform.position, transform.position)),
            new KeyValuePair<string, object>("Player_los", hit.transform != null ? hit.transform.gameObject == playerRef.gameObject : playerRef.currentNode == currentNode),
            new KeyValuePair<string, object>("Enemy_moving", currentPath.Count > 0)
            );
        Manager.Update();

        Move();
    }

    public static class Constants
    {
        //General
        public static float Max_Engage_Distance = 25f; //Distance to start attacking
        public static float Min_Engage_Distance = 15f; //Distance to start running

        //Roaming
        public static float Roam_Chance = 1f / 100f;
        public static int Max_Roam_Attempts = 5;

        //Running
        public static float WaitToRoam = 20;
        public static int RayCount = 10;

        //Attacking
        public static float ShotPerSecond = 5;

        public static float roamSpeed = 10;
        public static float runSpeed = 25;

        
    }



    class Enemy_Gun_Roam : FSM_State
    {
        Player player;
        Enemy_Gun Enemy;

        public Enemy_Gun_Roam(FSMv2 Manager) : base("Enemy_roam", Manager) { }

        public override void OnEntry()
        {
            //Debug.Log("Gunner | Enter | Roam");
            Enemy = (Enemy_Gun)Manager.getVariable("Enemy_main");
            player = (Player)Manager.getVariable("Player_main");
            Enemy.moveSpeed = Constants.roamSpeed;
            Enemy.setIndicator(new Color32(0, 153, 0, 255));
        }

        public override void OnExit()
        {
            //Debug.Log("Gunner | Exit | Roam");
        }

        public override void OnUpdate()
        {
            //if LOS & distance to player >= engage distance
            if ((bool)Manager.getVariable("Player_los") && (float)Manager.getVariable("Player_dist") >= Constants.Min_Engage_Distance && (float)Manager.getVariable("Player_dist") <= Constants.Max_Engage_Distance)
            {
                Manager.changeState("Enemy_attack");
                return;
            }

            //if LOS & distance to player < engage distance
            if ((bool)Manager.getVariable("Player_los") && (float)Manager.getVariable("Player_dist") < Constants.Min_Engage_Distance)
            {
                Manager.changeState("Enemy_run");
                return;
            }


            //if actively roaming
            if ((bool)Manager.getVariable("Enemy_moving"))
                return;

            // 0.125 chance
            if (Constants.Roam_Chance >= Random.value)
            {
                int Attempts = 0;
                while (Attempts < Constants.Max_Roam_Attempts)
                {
                    //generate random point to go to and start doing it
                    Vector2 inCircle = Random.insideUnitCircle; // insideUnitCircle is normalized
                    Vector3 point = new Vector3(inCircle.x, Enemy.transform.position.y, inCircle.y) * Random.Range(10, 20);

                    bool isAccessible = Enemy.GreedySearch(Enemy.currentNode, Enemy.findClosestWaypoint(point), out Enemy.PathingResults);
                    if (isAccessible)
                    {
                        Enemy.currentPath = Enemy.Path2Vector(Enemy.PathingResults);
                        break;
                    }
                    else
                    {
                        Attempts++;
                    }
                }

                //No fail incident, it will try to roam and if it fails to find a valid spot, it will continue to try till it dies
            }
        }
    }

    class Enemy_Gun_Attack : FSM_State
    {
        float TimeElapse = 0;
        Player player;
        Enemy_Gun Enemy;

        public Enemy_Gun_Attack(FSMv2 Manager) : base("Enemy_attack", Manager) { }

        public override void OnEntry()
        {
            //Debug.Log("Gunner | Enter | Attack");
            Enemy = (Enemy_Gun)Manager.getVariable("Enemy_main");
            player = (Player)Manager.getVariable("Player_main");
            TimeElapse = 0;
            Enemy.setIndicator(new Color32(255, 49, 49, 255));

            Instantiate<GameObject>((GameObject)Manager.getVariable("Enemy_bullet"), ((Transform)Manager.getVariable("Enemy_bullet_spawn")).position, Enemy.transform.rotation);
        }

        public override void OnExit()
        {
            //Debug.Log("Gunner | Exit | Attack");
        }

        public override void OnUpdate()
        {

            //Player gets too close, or we lose visual on them, run
            if ((float)Manager.getVariable("Player_dist") <= Constants.Min_Engage_Distance || !(bool)Manager.getVariable("Player_los"))
            {
                Manager.changeState("Enemy_roam");
                return;
            }

            //Look at the player
            //when looking start shooting

            if ((bool)Manager.getVariable("Enemy_moving"))
            {
                Enemy.currentPath.Clear();
                Enemy.PathingResults.Clear();
                Enemy.resetLookState();
            }


            Enemy.transform.LookAt(player.transform);

            //At least fire 1 bullet instantly
            if(TimeElapse >= Constants.ShotPerSecond)
            {
                TimeElapse = 0;
                //Instantiate bullet
                //Debug.Log("Bullet Fired");
                Instantiate<GameObject>((GameObject)Manager.getVariable("Enemy_bullet"), ((Transform)Manager.getVariable("Enemy_bullet_spawn")).position, Enemy.transform.rotation);
            }
            TimeElapse += Time.deltaTime;
        }
    }

    class Enemy_Gun_Run : FSM_State
    {

        WaypointGraph graph;
        float TimeElapsed = 0;
        Player player;
        Enemy_Gun Enemy;
        bool Hiding = false;

        public Enemy_Gun_Run(FSMv2 Manager) : base("Enemy_run", Manager) { }


        public override void OnEntry()
        {
            //Debug.Log("Gunner | Enter | Run");
            Enemy = (Enemy_Gun)Manager.getVariable("Enemy_main");
            player = (Player)Manager.getVariable("Player_main");
            graph = GameObject.FindGameObjectWithTag("waypoint graph").GetComponent<WaypointGraph>();

            Enemy.moveSpeed = Constants.runSpeed;
            Hiding = false;
            TimeElapsed = 0;
            Enemy.setIndicator(new Color32(255, 170, 51, 255));
        }

        public override void OnExit()
        {
            //Debug.Log("Gunner | Exit | Run");
            Hiding = false;
        }

        public override void OnUpdate()
        {
            TimeElapsed += Time.deltaTime;

            //Reset time if the enemy is seen
            if ((bool)Manager.getVariable("Player_los") && !(bool)Manager.getVariable("Enemy_moving"))
            {
                TimeElapsed = 0;
                FindHidingSpot();
                return;
            }
                

            //Out of engage distance, Not in player view & enough time has passed
            if ((float)Manager.getVariable("Player_dist") > Constants.Min_Engage_Distance && TimeElapsed >= Constants.WaitToRoam && !(bool)Manager.getVariable("Player_los"))
            {
                Manager.changeState("Enemy_roam");
                return;
            }

            if ((bool)Manager.getVariable("Enemy_moving"))
                return;

            if (Hiding)
                return;

            FindHidingSpot();
        }

        void FindHidingSpot()
        {
            Hiding = true;

            Vector3[] Surrounding_Hits = new Vector3[Constants.RayCount];
            Vector3[] Opposing_Hits = new Vector3[Constants.RayCount];
            LinkedNodes[] ClosestNodes = new LinkedNodes[Constants.RayCount];

            RaycastHit hit;
            Vector3 dir = Enemy.transform.forward;


            for (int x = 0; x < Constants.RayCount; x++)
            {
                if (Physics.Raycast(Enemy.transform.position, dir, out hit))
                {
                    Surrounding_Hits[x] = hit.point;
                    hit.collider.Raycast(new Ray(hit.point + (dir.normalized * 50), -dir), out hit, 50);
                    Opposing_Hits[x] = hit.point;
                    ClosestNodes[x] = findClosestViableWaypoint(Opposing_Hits[x]);
                }

                dir = Quaternion.AngleAxis(360f / Constants.RayCount, Vector3.up) * dir;
            }

            for(int i = 0; i < ClosestNodes.Length; i++)
            {
                if (Enemy.AStarSearch(Enemy.currentNode, ClosestNodes[i], out Enemy.PathingResults))
                {
                    Enemy.currentPath = Enemy.Path2Vector(Enemy.PathingResults);
                    break;
                }
            }
        }


        //Find the waypoint that is the closest to the opposing hit & out of player LOS
        private LinkedNodes findClosestViableWaypoint(Vector3 point)
        {
            int WaypointIndex = 0;
            float minDistance = Mathf.Infinity;
            for (int i = 0; i < graph.graphNodes.Length; i++)
            {
                //Dist check
                float dist = Vector3.Distance(point, graph.graphNodes[i].transform.position);
                if (dist > minDistance)
                    continue;

                //LOS check | Player to Node
                if (!Physics.Linecast(player.transform.position, graph.graphNodes[i].transform.position))//Linecast = true if collider between the points
                    continue;

                minDistance = dist;
                WaypointIndex = i;
            }
            return graph.graphNodes[WaypointIndex].GetComponent<LinkedNodes>();
        }
    }



}







[CustomEditor(typeof(Enemy_Gun))]
class EnemyStatus_Gun : Editor
{
    void OnSceneGUI()
    {
        var t = target as Enemy;
        Handles.Label(t.transform.position - (Vector3.up * 2), t.Manager.getCurrentState());
    }
}