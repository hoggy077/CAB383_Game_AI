using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;
using Random = UnityEngine.Random;

#region Workshop Original
//public class Drone : Enemy
//{

//    GameManager gameManager;

//    Rigidbody rb;

//    //Movement & Rotation Variables
//    public float speed = 50.0f;
//    private float rotationSpeed = 5.0f;
//    private float adjRotSpeed;
//    private Quaternion targetRotation;
//    public GameObject target;
//    public float targetRadius = 200f;


//    void Start()
//    {

//        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();

//        rb = GetComponent<Rigidbody>();
//    }


//    void Update()
//    {

//        //Acquire player if spawned in
//        if (gameManager.gameStarted)
//            target = gameManager.playerDreadnaught;

//        //Move towards valid targets
//        if (target)
//            MoveTowardsTarget();


//        BoidMechanics();
//    }

//    private void MoveTowardsTarget()
//    {
//        //Rotate and move towards target if out of range
//        if (Vector3.Distance(target.transform.position, transform.position) > targetRadius)
//        {

//            //Lerp Towards target
//            targetRotation = Quaternion.LookRotation(target.transform.position - transform.position);
//            adjRotSpeed = Mathf.Min(rotationSpeed * Time.deltaTime, 1);
//            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, adjRotSpeed);

//            rb.AddRelativeForce(Vector3.forward * speed * 20 * Time.deltaTime);
//        }
//    }


//    //Boid Steering/Flocking Variables
//    public float separationDistance = 25.0f;
//    public float cohesionDistance = 50.0f;
//    public float separationStrength = 250.0f;
//    public float cohesionStrength = 25.0f;
//    private Vector3 cohesionPos = new Vector3(0f, 0f, 0f);
//    private int boidIndex = 0;

//    private void BoidMechanics()
//    {
//        boidIndex++;
//        if (boidIndex >= gameManager.enemyList.Length)
//        {
//            Vector3 cohesiveForce = (cohesionStrength / Vector3.Distance(cohesionPos, transform.position)) * (cohesionPos - transform.position);
//            rb.AddForce(cohesiveForce);
//            boidIndex = 0;
//            cohesionPos.Set(0f, 0f, 0f);
//        }

//        Vector3 pos = gameManager.enemyList[boidIndex].transform.position;
//        Quaternion rot = gameManager.enemyList[boidIndex].transform.rotation;
//        float dist = Vector3.Distance(transform.position, pos);

//        if (dist > 0f)
//        {
//            if (dist <= separationDistance)
//            {
//                float scale = separationStrength / dist;
//                rb.AddForce(scale * Vector3.Normalize(transform.position - pos));
//            }
//            else if (dist < cohesionDistance && dist > separationDistance)
//            {
//                cohesionPos = cohesionPos + pos * (1f / (float)gameManager.enemyList.Length);
//                transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, 1f);
//            }
//        }
//    }
//}
#endregion

#region custom attempt
public class Drone : Enemy
{

    GameManager gameManager;

    Rigidbody rb;

    //Movement & Rotation Variables
    public float speed = 50.0f;
    private float rotationSpeed = 5.0f;
    private float adjRotSpeed;
    private Quaternion targetRotation;
    public GameObject target;
    public float targetRadius = 200f;

    [Header("Position hold")]
    public Vector3? holdPosition = null;
    public float Hold_Multiplier = 5f;



    #region Monobehaviour Inharitance
    void Start()
    {

        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
        
        MotherShip = gameManager.alienMothership.GetComponent<Mothership>();
        scoutPosition = MotherShip.transform.position;
        previousPoint = transform.position;

        rb = GetComponent<Rigidbody>();

        SeparationTarget = new Vector3();

        individualInfo = new Drone_characteristics()
        {
            Max_Charge = 300,//Random.Range(35, 50),
            Mining_Efficiency = Random.value,
            Battery_Efficiency = Random.Range(0.05f, 0.15f) //basically 5L/100km or 15L/100km
        };
        individualInfo.Charge = individualInfo.Max_Charge;
        individualInfo.debug_heuristic = individualInfo.get_Heuristic();
    }

    void Update()
    {

        if (MotherShip == null)
            base.takeDamage(50000);

        if (gameManager.gameStarted && State != BehaviourState.Fleeing)
        {
            target = gameManager.playerDreadnaught;
            State = BehaviourState.Attacking;
        }

        //Always recharge when in range of the mother ship
        if (Vector3.Distance(transform.position, MotherShip.transform.position) < targetRadius)
            individualInfo.Charge = individualInfo.Max_Charge;

        if(rb.velocity.magnitude > velocityFuelThreashold)
        {
            individualInfo.updateCharge(previousDistance);
        }


        //If the group ID is negative, the drone is an individual and wont flock
        if (Group_ID >= 0)
            BoidMechanics();




        switch (State)
        {
            case BehaviourState.Idle:
                Idle();
                break;

            case BehaviourState.Scouting:
                Scouting();
                break;

            case BehaviourState.Foraging:
                Foraging();
                break;

            case BehaviourState.Attacking:
                Attacking();
                break;

            case BehaviourState.Fleeing:
                Fleeing();
                break;
        }
    }
    #endregion

    #region Movement methods
    private void MoveTowardsTarget(Vector3 targetPoint)
    {
        //Rotate and move towards target if out of range
        if (Vector3.Distance(targetPoint, transform.position) > targetRadius)
        {
            RotateTowardsTarget(targetPoint);           

            rb.AddRelativeForce(Vector3.forward * speed * 20 * Time.deltaTime);
        }
    }

    private void RotateTowardsTarget(Vector3 targetPoint)
    {
        //Lerp Towards target
        targetRotation = Quaternion.LookRotation(targetPoint - transform.position);
        adjRotSpeed = Mathf.Min(rotationSpeed * Time.deltaTime, 1);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, adjRotSpeed);
    }
    #endregion

    #region Reactive flocking
    //Boid Steering/Flocking Variables
    [Header("Flocking")]
    public float separationDistance = 25.0f;
    public float cohesionDistance = 50.0f;
    public float separationStrength = 15.0f;
    public float cohesionStrength = 10.0f;

    [Header("Flock Identification")]
    public LayerMask DroneMask;
    public int Group_ID = 0;
    public bool isGroupLeader = false;

    [HideInInspector]
    public Vector3 SeparationTarget = new Vector3();
    [HideInInspector]
    public Vector3 CohesionTarget = new Vector3();


    //Boiding 
    public enum flockState
    {
        perform_flock,
        hold_position,
        disable_flock
    }
    private flockState MechanicSelect = flockState.perform_flock;

    private void BoidMechanics()
    {
        Collider[] localDrones = Physics.OverlapSphere(transform.position, cohesionDistance, (int)DroneMask);
        List<Drone> Separation_List = new List<Drone>();
        List<Drone> Cohesion_List = new List<Drone>();
        foreach (Collider drone in localDrones)
        {
            if (drone.gameObject == gameObject)
                continue;

            Drone result;
            bool success = drone.TryGetComponent<Drone>(out result);

            if (!success)
                continue;

            if (result.Group_ID != Group_ID)
                continue;

            #region Group Rules 1.0
            //If we're the the leader
            //  Assign the other drones leader to self
            //Else if we have a leader AND we're not in the general group AND the other drone hasn't got a leader reference
            //  Assign the other drones leader to our assigned leader
            //Else if we are in group general AND the other drone has an assigned leader
            //  Unassign the other drones leader

            //Above layout should let groups of drones larger than the cohesion radius of the leader
            //cascade from drone to drone, but also cascade a group of drones turned general back to not following a leader
            //The later should be helped by KeepUpWLeader which makes sure the drones will always move towards their leader if they're outside the cohesion zone

            // Pros
            //-----------
            // - If left behind, we continue to catchup till we reach the leader

            // Cons
            //-----------
            // - If the leader is reassigned mid way through catchup, it will chase them till it can be reset


            //if (isGroupLeader)
            //    result.GroupLeader = this;
            //else if (GroupLeader != null && Group_ID > 0 && result.GroupLeader == null)
            //    result.GroupLeader = GroupLeader;
            //else if (Group_ID <= 0 && result.GroupLeader != null)
            //    result.GroupLeader = null;
            #endregion

            #region Group rules 2.0
            // Pros
            //-----------
            // - If left behind, we continue to catch up
            // - If we can't catch up before the task is done, we are released and can instantly be assigned to do another task
            // - Means that in "field" flocking can occure with neighboring drones that are also abandoned should that edge case happen

            // Cons
            //-----------
            // - We may not reach the goal before the task is complete

            //If we're the leader AND their leader is null
            //  Assign the other drones leader to this
            //Else if their leader is null AND group ID > 0 AND our leader is NOT null
            //  Assign the other drones leader to this

            //Maintains group rule 1.0's cascade effect, but also allows inadequit drones to be released without needing contact with the leader
            //should all the drones in a group except 1 be returned to general (group 0) the exception drone will now also return to general

            if (isGroupLeader && result.GroupLeader == null)
                assignDrone_Leader(result, this); //Leader is sharing self
            else if (result.GroupLeader == null && Group_ID > 0 && GroupLeader != null)
                assignDrone_Leader(result, GroupLeader); //Drone is sharing to another outside the leaders range

            #endregion

            if (Vector3.Distance(result.transform.position, transform.position) <= separationDistance)
                Separation_List.Add(result);
            else
                Cohesion_List.Add(result);
        }



        #region Position Maintain - Disabled 
        //Position maintain
        //---------------------
        //This is here rather as it occures regardless of the state,
        //but also requires operation prior to the boid operations as to skip
        //flocking when moving to a target. KeepUPWLeader is there to force 
        //drones to maintain that selected flock by holding a given group,
        //but the below code is reliant on a group leader being chosen. 
        //Otherwise it should still flock on a set group ID

        //if (MechanicSelect == flockState.hold_position)
        //{
        //    if (isGroupLeader && target == null && Group_ID > 0)    //If we're a leader with no target and we're not not in general grouping
        //        holdPosition ??= transform.position;                //then record our position
        //    else
        //        holdPosition = null;


        //    if (isGroupLeader && holdPosition != null)                                                      //If we're a leader with a target hold position
        //        rb.AddForce(((Vector3)holdPosition - transform.position).normalized * Hold_Multiplier);     //move towards the hold position
        //}
        //else
        //    if (isGroupLeader && holdPosition == null && target != null)                               //we're a leader without a hold position, but with a target
        //    return;


        if (MechanicSelect == flockState.hold_position)
        {
            if (isGroupLeader && target == null && Group_ID > 0)    //If we're a leader with no target and we're not not in general grouping
                holdPosition ??= transform.position;                //then record our position
            else
                holdPosition = null;


            if (isGroupLeader && holdPosition != null)                                                      //If we're a leader with a target hold position
                rb.AddForce(((Vector3)holdPosition - transform.position).normalized * Hold_Multiplier);     //move towards the hold position
        }
        else if (MechanicSelect == flockState.disable_flock)
            return;

        #endregion




        //Boid operations
        bool hasSeparation = Separation(Separation_List.ToArray(), out SeparationTarget);
        if (hasSeparation)
            rb.AddForce(SeparationTarget.normalized * separationStrength);

        bool hasCohesion = Cohesion(Cohesion_List.ToArray(), out CohesionTarget);
        if (hasCohesion)
            rb.AddForce((CohesionTarget - transform.position).normalized * cohesionStrength);

        Quaternion rot;
        bool hasAligned = Alignment(Cohesion_List.ToArray(), out rot); //Always returns true if Cohesion_List.Count > 0
        if (hasAligned && target == null)
            transform.rotation = rot;

        //if (Group_ID > 0)
        //    KeepUpWLeader();
    }


    //Boid rules
    private bool Separation(Drone[] drones, out Vector3 resultPosition)
    {
        resultPosition = new Vector3();
        if (drones.Length <= 0)
            return false;

        foreach (Drone drn in drones)
            resultPosition += transform.position - drn.transform.position;

        return true;
    }

    private bool Cohesion(Drone[] drones, out Vector3 resultPosition)
    {
        resultPosition = new Vector3();

        if (drones.Length <= 0)
            return false;

        Vector3 pos = new Vector3();
        foreach (Drone drn in drones)
            pos += drn.transform.position;

        resultPosition = pos / drones.Length;
        return true;
    }

    private bool Alignment(Drone[] drones, out Quaternion resultRotation)
    {
        rb.angularVelocity = Vector3.zero;
        resultRotation = new Quaternion();

        if (drones.Length <= 0)
            return false;

        Vector3 newForward = new Vector3();
        foreach (Drone drn in drones) 
            newForward += drn.transform.forward;

        resultRotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(newForward / drones.Length), 0.01f);

        return true;
    }


    public Drone GroupLeader = null;
    private void KeepUpWLeader()
    {
        //This has the added benefit of making a drone follow the leader till they reach it 

        if (GroupLeader == null)
            return;

        if (Vector3.Distance(gameObject.transform.position, GroupLeader.transform.position) <= cohesionDistance)
            return;

        rb.AddForce((GroupLeader.transform.position - transform.position) * speed * Time.deltaTime);
    }


    #region Drone Utils
    static void assignDrone_Leader(Drone target, Drone leader) {
        if (target != leader)
        {
            //If statement is to avoid edge cases where the drone has a leader reference to itself

            target.GroupLeader = leader;
            leader.OnGroupRelease += target.releaseGroup;
            leader.onGroupAttack += target.groupAttacking;
        }
    }

    public void releaseGroup()
    {
        if (!isGroupLeader)
        {
            //reset the needed group variable & stop listening
            GroupLeader.OnGroupRelease -= releaseGroup;
            GroupLeader.onGroupAttack -= groupAttacking;
        }
        else
        {
            //Invoke the group reset
            //But also clear grouping specific mechanic, position hold
            OnGroupRelease.Invoke();
            holdPosition = null;
        }

        Group_ID = 0;
        GroupLeader = null;
        isGroupLeader = false;
    }

    public void LeaderTargeting(GameObject newTarget) => target = newTarget;

    public void groupAttacking(bool Attack_command) => hasAttackOrder = Attack_command;

    #endregion
    #endregion





    //Group info sharing - run by leader
    public delegate void onGroupRelease_();
    public event onGroupRelease_ OnGroupRelease;    //Universal group release

    public delegate void onGroupAttack_(bool Attack_command);
    public event onGroupAttack_ onGroupAttack;





    //Drone personal characteristics
    [Header("Characteristics")]
    public Drone_characteristics individualInfo;
    private Vector3 previousPoint = Vector3.zero;
    private float previousDistance
    {
        get
        {
            float d = Vector3.Distance(transform.position, previousPoint);
            previousPoint = transform.position;
            return d;
        }
    }
    public float velocityFuelThreashold = 15f;


    //FSM system
    public enum BehaviourState
    {
        Idle,
        Scouting,
        Foraging,
        Attacking,
        Fleeing
    }

    [Header("Finite State data")]
    public BehaviourState State = BehaviourState.Idle;



    #region Scouting
    [Header("Scouting")]
    public Mothership MotherShip;
    public Vector3 scoutPosition;
    public float point_Radius;

    private float scoutTimer;
    private float scoutTime = 10f;

    private float detectionTimer;
    private float detectionTime = 20f;

    public float Detection_radius = 400f;
    private float newResourceValue { get => bestResource != null ? bestResource.Resource_Count : 0; }
    public Asteroid bestResource;
    public LayerMask AsteroidMask;

    private List<Asteroid> BlackList = new List<Asteroid>();
    
    void Scouting()
    {
        //Scouting as a group:
        //-------------------
        // just dont, refer to general

        //Scouting in general:
        //-------------------
        // move till something is found

        #region Original
        //if (resourceObject)
        //{
        //    //Return to mothership, resouce found

        //    MoveTowardsTarget(MotherShip.transform.position);
        //    return;
        //}


        ////no resource found

        //if (Vector3.Distance(transform.position, scoutPosition) < Detection_radius && scoutTimer >= scoutTime)
        //{
        //    scoutPosition = MotherShip.transform.position + (Random.insideUnitSphere * point_Radius);
        //    scoutTime = 0;
        //}
        //else
        //    scoutTimer += Time.deltaTime;

        //MoveTowardsTarget(scoutPosition);

        //Debug.DrawLine(transform.position, scoutPosition, Color.yellow);
        #endregion


        //If we suddenly cant reach the mother ship, due to fuel limitation, turn around a return to recharge immediately
        if (!individualInfo.canReach(Vector3.Distance(transform.position, MotherShip.transform.position), false))
        {
            MoveTowardsTarget(MotherShip.transform.position);

            if (Vector3.Distance(transform.position, MotherShip.transform.position) <= targetRadius)
                individualInfo.Charge = individualInfo.Max_Charge;
            return;
        }


        //We found a good resource in a group
        if (bestResource != null)
        {
            MoveTowardsTarget(MotherShip.transform.position);

            if(Vector3.Distance(transform.position, MotherShip.transform.position) <= targetRadius)
            {
                MotherShip.Resource_Collection.Add(bestResource);
                resetScout();
            }

            return;
        }



        //Got close & the timer exceeded
        if(Vector3.Distance(transform.position, scoutPosition) < Detection_radius && scoutTimer >= scoutTime)
        {
            //If we were flocking before this, stop flocking
            if (Group_ID >= 0)
                Group_ID = -1;

            scoutPosition = MotherShip.transform.position + (Random.insideUnitSphere * point_Radius * Random.value);
            return;
        }

        //We didn't get close, but the timer exceeded, so do a mid way test
        if(detectionTimer >= detectionTime)
        {
            Collider[] Asteroids = Physics.OverlapSphere(transform.position, Detection_radius, AsteroidMask);
            if (Asteroids.Length > 0)
            {
                bestResource = null;
                for(int i = 0; i < Asteroids.Length; i++)
                {
                    Asteroid tmp = Asteroids[i].GetComponent<Asteroid>();

                    if (BlackList.Contains(tmp))
                        continue;

                    if (MotherShip.Resource_Collection.Contains(tmp))
                    {
                        BlackList.Add(tmp);
                        continue;
                    }

                    if(tmp.Resource_Count > newResourceValue && tmp.Resource_Count / tmp.Max_Resource_Count > Asteroid_Report_Threshold)
                        bestResource = tmp;
                }
            }
            detectionTimer = 0;
            return;
        }

        MoveTowardsTarget(scoutPosition);
        Debug.DrawLine(transform.position, scoutPosition);
        scoutTimer += Time.deltaTime;
        detectionTimer += Time.deltaTime;
    }

    public void resetScout()
    {
        bestResource = null;
        MotherShip.ReleaseScout(this);
        State = BehaviourState.Idle;
        Group_ID = 0;
        BlackList.Clear();
    }
    #endregion


    #region Foraging
    [Header("Foraging")]
    public Asteroid ForagingTarget;

    [Range(0, 1)]
    public float Asteroid_Report_Threshold = 0.3f;
    private bool threshHold_return = false;

    public float resourcesCollected = 0;
    public float Max_Resources = 15;
    private bool eliteScouting = false;
    private bool eliteScouting_done = false;

    public float eliteScout_Radius = 150;
    public float collection_Radius = 150;

    private List<int> AsteroidsTagged = new List<int>();

    void Foraging()
    {
        //Foraging as a group:
        //--------------------
        // There is none

        //foraging as a general:
        //----------------------
        // Just get the stuffs


        //Move to assigned asteroid
        //when in range, collect till threshold is met, or asteroid runs out of materials
        //if normal drone
        //  return to mothership and hand resource over
        //  get assigned a new asteroid
        //  if the resource count of the asteroid is below the threshold, report it
        //else if elite
        //  scout for a little while using the asteroid as the center point
        //  if anything found or search time elapsed, return to the ship & report

        //We have a target
        if (!ForagingTarget)
            return;

        Group_ID = -1;

        //We know the asteroid, we scouting + reporting new asteroids
        if (individualInfo.isElite)
        {
            if (eliteScouting && bestResource == null)
            {
                if (Vector3.Distance(scoutPosition, transform.position) > Detection_radius)
                {
                    MoveTowardsTarget(scoutPosition);
                }
                else
                {
                    Collider[] Asteroids = Physics.OverlapSphere(transform.position, Detection_radius, AsteroidMask);
                    if (Asteroids.Length > 0)
                    {
                        bestResource = null;
                        for (int i = 0; i < Asteroids.Length; i++)
                        {
                            Asteroid tmp = Asteroids[i].GetComponent<Asteroid>();

                            if (MotherShip.Resource_Collection.Contains(tmp))
                                continue;

                            //The asteroid is better and has enough resources to mine
                            if (tmp.Resource_Count > newResourceValue && tmp.Resource_Count / tmp.Max_Resource_Count > Asteroid_Report_Threshold)
                            {
                                //was dead, aint anymore
                                if (tmp.deadCount >= 5)
                                    tmp.deadCount = 0;

                                bestResource = tmp;
                            }
                        }
                    }
                    else
                    {
                        eliteScouting_done = true;
                        eliteScouting = false;
                    }
                    return;
                }
            }
            else if ((eliteScouting && bestResource != null) || (eliteScouting_done && !eliteScouting))
            {
                //found a new & better asteroid
                if (Vector3.Distance(MotherShip.transform.position, transform.position) > collection_Radius)
                {
                    //move to the mothership
                    MoveTowardsTarget(MotherShip.transform.position);
                }
                else
                {
                    if (bestResource == null)
                    {
                        ForagingTarget.deadCount++;
                        MotherShip.evaluateCollection();
                    }
                    else
                    {
                        if (!MotherShip.Resource_Collection.Contains(bestResource))
                            MotherShip.Resource_Collection.Add(bestResource);
                    }

                    eliteScouting_done = false;
                    eliteScouting = false;
                    bestResource = null;
                    return;
                }
            }
        }

        
        //Going back and recharging
        if (!individualInfo.canReach(Vector3.Distance(transform.position, MotherShip.transform.position), false))
        {
            //convert some resources into charge to last longer
            if(resourcesCollected > 10) 
            {
                float conversion = (resourcesCollected - 10) * individualInfo.Battery_Efficiency;
                resourcesCollected = resourcesCollected - (resourcesCollected - 10);
                individualInfo.Charge += conversion;
                return;
            }

            MoveTowardsTarget(MotherShip.transform.position);

            if (Vector3.Distance(transform.position, MotherShip.transform.position) <= collection_Radius)
                individualInfo.Charge = individualInfo.Max_Charge;
            return;
        }



        //we're not scouting, the asteroid is dead, or we're maxed on resources
        if ((ForagingTarget.deadCount >= 5 || resourcesCollected >= Max_Resources || threshHold_return) && !eliteScouting)
        {
            //return to base
            if (Vector3.Distance(MotherShip.transform.position, transform.position) > collection_Radius)
            {
                //move to the mothership
                MoveTowardsTarget(MotherShip.transform.position);
            }
            else
            {
                //we're at the mothership

                //the asteroid is dead AF
                if (ForagingTarget.deadCount >= 5)
                {
                    MotherShip.evaluateCollection();
                    State = BehaviourState.Idle;
                }

                MotherShip.receiveResource(resourcesCollected);
                resourcesCollected = 0;
                AsteroidsTagged.Clear();
                threshHold_return = false;
            }
            return;
        }


        //Asteroid move towards
        if (Vector3.Distance(ForagingTarget.transform.position, transform.position) > collection_Radius)
        {
            //move to the asteroid
            MoveTowardsTarget(ForagingTarget.transform.position);
        }
        else
        {
            //we're at the asteroid
            float remaining = ForagingTarget.Resource_Count / ForagingTarget.Max_Resource_Count;

            if (remaining <= Asteroid_Report_Threshold && !AsteroidsTagged.Contains(ForagingTarget.gameObject.GetInstanceID()))
            {
                ForagingTarget.deadCount++;
                AsteroidsTagged.Add(ForagingTarget.gameObject.GetInstanceID());
                threshHold_return = true;
            }

            if (ForagingTarget.deadCount < 5 && resourcesCollected <= Max_Resources && ForagingTarget.Resource_Count >= ForagingTarget.Max_Resource_Count * Asteroid_Report_Threshold)
            {
                resourcesCollected += individualInfo.Mining_Efficiency;
                ForagingTarget.Resource_Count -= individualInfo.Mining_Efficiency;
            }
            

            if (individualInfo.isElite)
            {
                eliteScouting = true;
                scoutTimer = 0;
                scoutPosition = ForagingTarget.transform.position + (Random.insideUnitSphere * eliteScout_Radius);
            }
        }
    }
    #endregion



    void Idle()
    {
        //Idle as a set group:
        //---------------------
        // Leader   | Default to position maintain. Requires boiding to be ignored so refer to "Position maintain" in boid mechanics
        // Standard | flock to the leader if there is one, otherwise move to the leader and flock 

        //Idle as a general:
        //---------------------
        // Standard | Dont do anything

        // Transition to    | Transition Req                | 
        //---------------------------------------------------
        // Attack           | player dist < Attack radius   |


        //Group_ID = 0;


        //if (isGroupLeader && target == null)
        //    MechanicSelect = flockState.hold_position;
        //else
        //    MechanicSelect = flockState.perform_flock;

        
    }

    #region Attacking
    [Header("Attacking")]
    public float distanceRatio = 0.05f;
    Vector3 targetVelocity { get {

            if (!target)
                return Vector3.zero;

            Vector3 v = (target.transform.position - targetPreviousPoint) / Time.deltaTime;
            targetPreviousPoint = target.transform.position;

            return v;
        } }
    Vector3 targetPreviousPoint;
    Vector3 predictionPoint;

    public LineRenderer laserFire;
    public GameObject laserFire_point;
    
    public float FireTiming = 20f;
    private float FireTimer;
    private bool hasAttackOrder = false;

    void Attacking()
    {
        //Attacking as a group:
        //--------------------
        // Leader   | use general rules, except on death, release the group, and reform on a new leader
        // Standard | use general rules

        //Attacking in general:
        //--------------------
        // if player is in attack range, get to work
        // if player is out of range, move to intercept point
        // if in a group with a leader, follow the leader
        // if in a group without a leader, abandon the group to intercept


        //targetVelocity has a getter that handles the calculation and updating of targetPreviousPoint
        predictionPoint = (target.transform.position + distanceRatio * Vector3.Distance(transform.position, target.transform.position) * targetVelocity) + (Vector3.up * 5);
        Debug.DrawLine(transform.position, predictionPoint, Color.red);


        #region Original

        //if (Vector3.Distance(transform.position, predictionPoint) > targetRadius)
        //    MoveTowardsTarget(predictionPoint);
        //else
        //{
        //    RotateTowardsTarget(target.transform.position);

        //    FireTimer += Time.deltaTime;

        //    //fire rate timer + firing
        //    if (FireTimer >= FireTiming)
        //    {
        //        Instantiate(alienFire, transform.position, transform.rotation);
        //        FireTimer = 0;
        //        return;
        //    }

        //    //Drone has 30% hp remaining
        //    if(health <= MaxHealth * Flee_Percent)
        //    {
        //        resetTemporaries();
        //        State = BehaviourState.Fleeing;
        //    }
        //}
        #endregion


        #region Group logic
        if (isGroupLeader && Group_ID > 0)
        {
            //We are a leader of a group

            // Disable flocking to avoid being dragged by trailing group members
            // Move towards target point
            // On reaching the point, share the target to the rest of the group & hold position


            //Move to intercept
            if (Vector3.Distance(transform.position, predictionPoint) > targetRadius)
            {
                MechanicSelect = flockState.disable_flock;
                MoveTowardsTarget(predictionPoint);

                laserFire.positionCount = 0;
                laserFire.SetPositions(new Vector3[]{});
            }
            else
            {
                //Reached, so indicate group attacking & hold position
                MechanicSelect = flockState.hold_position;
                hasAttackOrder = true;
                onGroupAttack.Invoke(true);

                RotateTowardsTarget(target.transform.position);

                //fire rate timer + firing
                if (FireTimer >= FireTiming)
                {
                    //Instantiate(alienFire, transform.position, transform.rotation);

                    laserFire.positionCount = 2;
                    laserFire.SetPositions(new Vector3[]
                    {
                        laserFire_point.transform.position,
                        gameManager.playerDreadnaught.transform.position
                    });


                    FireTimer = 0;
                    return;
                }
                FireTimer += Time.deltaTime;
            }


            if (shouldFlee())
            {
                onGroupAttack.Invoke(false);
                //releaseGroup();

                resetTemporaries();
                State = BehaviourState.Fleeing;
            }

            return;
        }

        if (!isGroupLeader && Group_ID > 0 && GroupLeader != null)
        {
            //We aren't a leader of the group

            //if signaled to attack
            //  begin attacking
            //else
            //  Follow the leader till we are shared our target

            //RotateTowardsTarget(target.transform.position);
            transform.rotation = GroupLeader.transform.rotation;


            if (hasAttackOrder)
            {
                if (FireTimer >= FireTiming)
                {
                    laserFire.positionCount = 2;
                    laserFire.SetPositions(new Vector3[]
                    {
                        laserFire_point.transform.position,
                        gameManager.playerDreadnaught.transform.position
                    });

                    FireTimer = 0;
                    return;
                }
                else
                {
                    laserFire.positionCount = 0;
                    laserFire.SetPositions(new Vector3[] { });
                }
                FireTimer += Time.deltaTime;
            }
            else
                KeepUpWLeader();


            if (shouldFlee())
            {
                releaseGroup();

                resetTemporaries();
                State = BehaviourState.Fleeing;
            }

            return;
        }
        #endregion

        #region Individual logic
        //order allows no return
        if (Group_ID > 0 && GroupLeader == null)
            //In a group, but no leader, abandon the group and join general
            Group_ID = 0;

        if (Group_ID <= 0)
        {
            //general or individual

            if (Vector3.Distance(transform.position, predictionPoint) > targetRadius)
                MoveTowardsTarget(predictionPoint);
            else
            {
                RotateTowardsTarget(target.transform.position);

                //fire rate timer + firing
                if (FireTimer >= FireTiming)
                {
                    laserFire.positionCount = 2;
                    laserFire.SetPositions(new Vector3[]
                    {
                        laserFire_point.transform.position,
                        gameManager.playerDreadnaught.transform.position
                    });

                    FireTimer = 0;
                    return;
                }
                else
                {
                    laserFire.positionCount = 0;
                    laserFire.SetPositions(new Vector3[] { });
                }
                FireTimer += Time.deltaTime;
            }

            if (shouldFlee())
            {
                resetTemporaries();
                State = BehaviourState.Fleeing;
            }
        }
        #endregion
    }

    float Determine_collective_Health(out float sumMax)
    {
        //We dont care if the neighboring drones are in the same group or a different group
        Collider[] locals = Physics.OverlapSphere(transform.position, cohesionDistance, DroneMask);
        float totalHealth = 0;
        sumMax = 0;
        foreach (Collider c in locals)
        {
            Drone drn = c.gameObject.GetComponent<Drone>();
            totalHealth += drn.health;
            sumMax += drn.MaxHealth;                            //Should we decide they have random varying max healths
        }
        return totalHealth;
    }

    bool shouldFlee()
    {
        float sumMax = 0;
        float surroundingHealth = Determine_collective_Health(out sumMax);
        return surroundingHealth <= sumMax * Flee_Percent || health < MaxHealth * Flee_Percent;
    }
    #endregion

    #region Fleeing
    [Header("Fleeing")]
    public float RepairTime = 20f;
    private float RepairTimer;
    public float Flee_Percent = 0.3f;

    void Fleeing()
    {
        //Return to the mothership and repair
        if (Vector3.Distance(transform.position, MotherShip.transform.position) > targetRadius)
        {
            MoveTowardsTarget(MotherShip.transform.position);
            RepairTimer = 0;

            if (isGroupLeader)
                MechanicSelect = flockState.hold_position;
            else
                Group_ID = -1;

        }
        else
        {
            RepairTimer += Time.deltaTime;

            if (RepairTime >= RepairTimer)
            {
                health = MaxHealth;
                State = BehaviourState.Attacking;
            }
        }
    }
    #endregion

    void resetTemporaries()
    {
        RepairTimer = 0;
        FireTimer = 0;
        scoutTimer = 0;
        hasAttackOrder = false;
    }



    //For group disband on leader death
    public override void onDeath()
    {
        if (GroupLeader)
            releaseGroup();
    }
}





[Serializable]
public class Drone_characteristics
{
    public float debug_heuristic;

    public float Max_Charge;
    public float Charge;                   //Decreases with distance. Bee's have a "social stomach" which they use to feed exhausted bees when returning to the hive. Might be worth considering
    public float Mining_Efficiency;        //Represents the rate at which resources are taken from asteroids
    public float Battery_Efficiency;       //The cost of moving X unit per 1 distance units = basically L per 1km = [0,1]

    public bool isElite = false;


    public float calc_fuel_cost(float distance) => distance * Battery_Efficiency;
    public bool canReach(float distance, bool roundTrip)
    {

        if(calc_fuel_cost(distance) >= (roundTrip ? Charge / 2 : Charge))
            return false;
        return true;
    }

    //Absolute maximum range
    public float maxDistance_fullTank() => Max_Charge / Battery_Efficiency;
    public float maxDistance_CurrentTank() => Charge / Battery_Efficiency;


    public float get_Heuristic() => 1 - ((Battery_Efficiency - 0.05f) / (0.15f - 0.05f)) - Mining_Efficiency;


    public void updateCharge(float distance)
    {
        //Debug.Log(distance);
        Charge -= distance * Battery_Efficiency;
    }
}







#if UNITY_EDITOR
[CustomEditor(typeof(Drone))]
class BoidScene_Editor : Editor
{
    private void OnSceneGUI()
    {
        Drone t = target as Drone;

        EditorGUI.BeginChangeCheck();
        Handles.color = Color.yellow;
        float Cohesion = Handles.RadiusHandle(Quaternion.Euler(Vector3.up), t.transform.position, t.cohesionDistance);

        Handles.color = Color.red;
        float Separation = Handles.RadiusHandle(Quaternion.Euler(Vector3.up), t.transform.position, t.separationDistance);


        Handles.Label(t.transform.position, $"State: {t.State.ToString()}");
        Handles.Label(t.transform.position + -(Vector3.up * 5), $"State: {t.individualInfo.Charge}");


        if (EditorGUI.EndChangeCheck())
        {
            t.separationDistance = Separation;
            t.cohesionDistance = Cohesion;
        }

        if (Event.current.type == EventType.Repaint)
        {
            Handles.color = Color.green;
            Handles.DotHandleCap(0, t.CohesionTarget, Quaternion.identity, 0.5f, EventType.Repaint);

            Handles.color = new Color(255, 128, 0);
            Handles.DotHandleCap(0, t.transform.position + t.SeparationTarget, Quaternion.identity, 0.5f, EventType.Repaint);
        }
    }

    public override void OnInspectorGUI()
    {
        Drone t = target as Drone;
        base.OnInspectorGUI();
        if(GUILayout.Button("Release Group"))
        {
            if (t.isGroupLeader)
                t.releaseGroup();
        }
    }
}
#endif
#endregion