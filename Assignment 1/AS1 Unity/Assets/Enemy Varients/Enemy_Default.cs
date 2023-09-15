using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Enemy_Default : Enemy
{
    //                      Variables
    //
    //[ Variable Name       | Type          | Content            ]
    //[---------------------|---------------|--------------------]
    //[ Player_los          | Bool          | LOS to player      ]
    //[ Enemy_moving        | bool          | Is roaming         ]
    //[ Enemy_main          | Enemy_Default | Script Reference   ]
    //[ Player_main         | Player        | Script Reference   ]


    //                  FSM (NFA)
    //
    //[ State   | Transition Condition  | New State ]
    //[---------|-----------------------|-----------]
    //[ Roam    | LOS                   | Attack    ]
    //[ Attack  | Player reached        | Hide      ]
    //[ Hide    | Time Elapsed + LOS    | Roam      ]

    public override void FSM_Setup()
    {
        Manager.updateVariable("Player_los", false);
        Manager.updateVariable("Enemy_moving", false);
        Manager.updateVariable("Enemy_main", this);
        Manager.updateVariable("Player_main", playerRef);

        Manager.setDefaultState("Enemy_roam");
        Manager.addState(new Enemy_Default_Roam(Manager)); //Roam
        Manager.addState(new Enemy_Default_Attack(Manager)); //Attack
        Manager.addState(new Enemy_Default_Hide(Manager)); //Run
    }

    public static class Constants
    {
        //General

        //Roaming
        public static float Roam_Chance = 1f / 100f;
        public static int Max_Roam_Attempts = 5;

        //Running
        public static float WaitToRoam = 20;
        public static int RayCount = 10;

        //Attacking
        public static float MaximumAttackDuration = 10;

        //speeds
        public static float roamSpeed = 10;
        public static float runSpeed = 25;
        public static float attackSpeed = 15;


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




    class Enemy_Default_Roam : FSM_State
    {
        Player player;
        Enemy_Default Enemy;

        public Enemy_Default_Roam(FSMv2 Manager) : base("Enemy_roam", Manager) { }

        public override void OnEntry()
        {
            //Debug.Log("Default |  Enter | Roam");
            Enemy = (Enemy_Default)Manager.getVariable("Enemy_main");
            player = (Player)Manager.getVariable("Player_main");
            Enemy.moveSpeed = Constants.roamSpeed;
            Enemy.setIndicator(new Color32(0, 153, 0, 255));
        }

        public override void OnExit()
        {
            //Debug.Log("Default |  Exit | Roam");
        }

        public override void OnUpdate()
        {
            //if LOS & distance to player
            if ((bool)Manager.getVariable("Player_los"))
            {
                Manager.changeState("Enemy_attack");
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

    class Enemy_Default_Attack : FSM_State
    {
        float TimeElapse = 0;
        Player player;
        Enemy_Default Enemy;
        LinkedNodes Goal = new LinkedNodes() { index = -1 };

        public Enemy_Default_Attack(FSMv2 Manager) : base("Enemy_attack", Manager) { }

        public override void OnEntry()
        {
            //Debug.Log("Default |  Enter | Attack");
            Enemy = (Enemy_Default)Manager.getVariable("Enemy_main");
            player = (Player)Manager.getVariable("Player_main");
            TimeElapse = 0;
            Enemy.setIndicator(new Color32(255, 49, 49, 255));
            Enemy.moveSpeed = Constants.attackSpeed;
        }

        public override void OnExit()
        {
            //Debug.Log("Default |  Exit | Attack");
        }

        public override void OnUpdate()
        {
            TimeElapse += Time.deltaTime;
            if (Goal != player.currentNode) {
                //player has moved node since last update
                Goal = player.currentNode;
                bool isAccessible = Enemy.AStarSearch(Enemy.currentNode, Goal, out Enemy.PathingResults);
                if (isAccessible)
                    Enemy.currentPath = Enemy.Path2Vector(Enemy.PathingResults);
                else
                {
                    //We couldn't path to them, the attack failed
                    Manager.changeState("Enemy_roam");
                    return;
                }
            }



            if (TimeElapse >= Constants.MaximumAttackDuration)
            {
                Manager.changeState("Enemy_roam");
                return;
            }

            //if we have a path, and the player hasn't moved
            if ((bool)Manager.getVariable("Enemy_moving") && Goal == player.currentNode)
                return; //continue the path


            //We reached the player, the attack was successful
            if(player.currentNode == Enemy.currentNode)
            {
                Manager.changeState("Enemy_hide");
                return;
            }
        }
    }

    class Enemy_Default_Hide : FSM_State
    {

        WaypointGraph graph;
        float TimeElapsed = 0;
        Player player;
        Enemy_Default Enemy;
        bool Hiding = false;

        public Enemy_Default_Hide(FSMv2 Manager) : base("Enemy_hide", Manager) { }


        public override void OnEntry()
        {
            //Debug.Log("Default |  Enter | Run");
            Enemy = (Enemy_Default)Manager.getVariable("Enemy_main");
            player = (Player)Manager.getVariable("Player_main");
            graph = GameObject.FindGameObjectWithTag("waypoint graph").GetComponent<WaypointGraph>();

            Enemy.moveSpeed = Constants.runSpeed;
            Hiding = false;
            TimeElapsed = 0;
            Enemy.setIndicator(new Color32(255, 170, 51, 255));
        }

        public override void OnExit()
        {
            //Debug.Log("Default |  Exit | Run");
            Hiding = false;
        }

        public override void OnUpdate()
        {
            TimeElapsed += Time.deltaTime;

            //Reset time if the enemy is seen & find new hiding spot
            if ((bool)Manager.getVariable("Player_los") && !(bool)Manager.getVariable("Enemy_moving") && Hiding)
            {
                Manager.changeState("Enemy_attack");
                //FindHidingSpot();
                return;
            }


            //Out of engage distance, Not in player view & enough time has passed
            if (TimeElapsed >= Constants.WaitToRoam && !(bool)Manager.getVariable("Player_los"))
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

            for (int i = 0; i < ClosestNodes.Length; i++)
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





[CustomEditor(typeof(Enemy_Default))]
class EnemyStatus_Default : Editor
{
    void OnSceneGUI()
    {
        var t = target as Enemy;
        Handles.Label(t.transform.position - (Vector3.up * 2), t.Manager.getCurrentState());
    }
}