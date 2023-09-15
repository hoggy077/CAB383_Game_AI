using Panda;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

public class MechAIDecisions_Thomas : MechAI
{
    public string botName = "Thomas - Test Bot";


    Animator anim;

    public MechSystems mechSystem;
    public MechAIMovement mechAIMovement;
    public MechAIAiming mechAIAiming;
    public MechAIWeapons mechAIWeapons;



    //Roam Variables
    public GameObject[] patrolPoints;
    private int patrolIndex = 0;

    //Attack Variables
    private float attackTime = 3.5f;
    private float attackTimer;


    //Flee Variables
    public GameObject fleeTarget;


    void Start()
    {
        //Collect Mech and AI Systems
        mechSystem = GetComponent<MechSystems>();
        mechAIMovement = GetComponent<MechAIMovement>();
        mechAIAiming = GetComponent<MechAIAiming>();
        mechAIWeapons = GetComponent<MechAIWeapons>();

        //Animator for sprinting
        anim = GetComponent<Animator>();


        //Roam State Startup Declarations
        patrolPoints = GameObject.FindGameObjectsWithTag("Patrol Point");
        patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);


        frustumPointA = mechAIAiming.frustumPointA;
        frustumPointB = mechAIAiming.frustumPointB;
        rayCastPoint = mechAIAiming.rayCastPoint;

        maxEnergy = mechSystem.energy;
        maxHealth = mechSystem.health;
    }







    #region Original
    /*
    [Task]
    bool HasAttackTarget()
    {
        if (attackTarget)
            return true;
        return false;
    }

    [Task]
    bool TargetLOS() => mechAIAiming.LineOfSight(attackTarget);






    [Task]
    private void Roam()
    {
        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f)
        {
            patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        }
        else
        {
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        }

        mechAIAiming.RandomAimTarget(patrolPoints);

        checkForCrate();
    }


    [Task]
    private void Attack()
    {

        //If there is a target, set it as the aimTarget 
        if (attackTarget && mechAIAiming.LineOfSight(attackTarget))
        {

            //Child object correction - wonky pivot point
            mechAIAiming.aimTarget = attackTarget.transform.GetChild(0).gameObject;

            //Move Towards attack Target
            if (Vector3.Distance(transform.position, attackTarget.transform.position) >= 45.0f)
            {
                mechAIMovement.Movement(attackTarget.transform.position, 45);
            }
            //Otherwise "strafe" - move towards random patrol points at intervals
            else if (Vector3.Distance(transform.position, attackTarget.transform.position) < 45.0f && Time.time > attackTimer)
            {
                patrolIndex = Random.Range(0, patrolPoints.Length - 1);
                mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 2);
                attackTimer = Time.time + attackTime + Random.Range(-0.5f, 0.5f);
            }

            //Track position of current target to pursue if lost
            pursuePoint = attackTarget.transform.position;
        }
    }


    [Task]
    void Pursue()
    {

        //Move towards last known position of attackTarget
        if (Vector3.Distance(transform.position, pursuePoint) > 3.0f)
        {
            mechAIMovement.Movement(pursuePoint, 1);
            mechAIAiming.RandomAimTarget(patrolPoints);
        }
        //Otherwise if reached and have not re-engaged, reset attackTarget and Roam
        else
        {
            attackTarget = null;
            patrolIndex = Random.Range(0, patrolPoints.Length - 1);
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        }
    }


    [Task]
    void Flee()
    {

        //If there is an attack target, set it as the aimTarget 
        if (attackTarget && mechAIAiming.LineOfSight(attackTarget))
        {
            //Child object correction - wonky pivot point
            mechAIAiming.aimTarget = attackTarget.transform.GetChild(0).gameObject;
        }
        else
        {
            //Look at random patrol points
            mechAIAiming.RandomAimTarget(patrolPoints);
        }

        //Move towards random patrol points <<< This could be drastically improved!
        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f)
        {
            patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        }
        else
        {
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        }
    }


    public override void TakingFire(int origin)
    {

        //If not own damage and no current attack target, find attack target
        if (origin != mechSystem.ID && !attackTarget)
        {
            foreach (GameObject target in mechAIAiming.targets)
            {
                if (target)
                {
                    if (origin == target.GetComponent<MechSystems>().ID)
                    {
                        attackTarget = target;
                        mechAIAiming.aimTarget = target;
                    }
                }
            }
        }
    }


    [Task]
    private bool StatusCheck()
    {

        float status = mechSystem.health + mechSystem.energy + (mechSystem.shells * 7) + (mechSystem.missiles * 10);

        if (status > 1500)
            return false;
        else
            return true;
    }


    private void FiringSystem()
    {

        //Lasers - Enough energy and within a generous firing angle
        if (mechSystem.energy > 10 && mechAIAiming.FireAngle(20))
            mechAIWeapons.Lasers();

        //Cannons - Moderate distance, enough shells and tight firing angle
        if (Vector3.Distance(transform.position, attackTarget.transform.position) > 25
            && mechSystem.shells > 4 && mechAIAiming.FireAngle(15))
            mechAIWeapons.Cannons();

        //Laser Beam - Strict range, plenty of energy and very tight firing angle
        if (Vector3.Distance(transform.position, attackTarget.transform.position) > 20
            && Vector3.Distance(transform.position, attackTarget.transform.position) < 50
            && mechSystem.energy >= 300 && mechAIAiming.FireAngle(10))
            mechAIWeapons.laserBeamAI = true;
        else
            mechAIWeapons.laserBeamAI = false;


        //Missile Array - Long Range, enough ammo, very tight firing angle
        if (Vector3.Distance(transform.position, attackTarget.transform.position) > 50
            && mechSystem.missiles >= 18 && mechAIAiming.FireAngle(5))
            mechAIWeapons.MissileArray();

    }
    */
    #endregion


    #region LOS clone | Stolen from the aim system cause fuck you, thats why
    Vector2 a, b, c;
    public GameObject frustumPointA, frustumPointB, rayCastPoint;
    public bool checkInView(GameObject target, Predicate<RaycastHit> LOS_condition) => checkInView(target.transform.position, LOS_condition);
    public bool checkInView(Vector3 target, Predicate<RaycastHit> LOS_condition) => checkIsBarycentric(target) & LineOfSight(target, LOS_condition);



    private bool Barycentric(Vector2 a, Vector2 b, Vector2 c, Vector2 tar)
    {

        Vector2 v0 = c - a, v1 = b - a, v2 = tar - a;

        // Compute dot products
        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);

        // Compute barycentric coordinates
        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;

        // return if point is in triangle
        return ((u >= 0) && (v >= 0) && (u + v < 1));
    }
    public bool checkIsBarycentric(GameObject target) => checkIsBarycentric(target.transform.position);
    public bool checkIsBarycentric(Vector3 target)
    {
        a = new Vector2(frustumPointA.transform.position.x, frustumPointA.transform.position.z);
        b = new Vector2(frustumPointB.transform.position.x, frustumPointB.transform.position.z);
        c = new Vector2(rayCastPoint.transform.position.x, rayCastPoint.transform.position.z);

        Vector2 target2d = new Vector2(target.x, target.z);
        return Barycentric(a, b, c, target2d);
    }


    public bool LineOfSight(GameObject target, Predicate<RaycastHit> LOS_condition) => LineOfSight(target.transform.position, LOS_condition);
    public bool LineOfSight(Vector3 target, Predicate<RaycastHit> LOS_condition)
    {
        RaycastHit hit;
        if (Physics.Raycast(rayCastPoint.transform.position, (target - rayCastPoint.transform.position).normalized, out hit, 100.0f, 31, QueryTriggerInteraction.Collide))
            return LOS_condition.Invoke(hit);

        return false;
    }
    #endregion


    #region concepts
    /*
    concept refinements brought to you by (people I forced to play the game in my basement for ideas):
        Rompuslord,
        That guy whos job used to be lighting oil based street lamps,
        Jim Carry's The Mask & Son of The Mask,
        Dwane The Wok Johnson (The fortnite version)
        David (not conroy, that one guy who makes the music but cant do maths)
        Baro Ki'Teer
    */





    //Footstep concepts:
    // for all bots near by
    //  -> find their closest footsteps
    //   if there is none and we have 1
    //    -> skip
    //   if there is none
    //    -> skip
    //   if there is a current duplicate
    //    -> skip (edge case 1 is hiding behind a wall not moving, and 1 walks by, both could return the same footstep)
    //   if there is 1 & we have 1
    //    -> update entry

    //Investigate Usage:
    // on the closest POI
    //  -> Move towards it (walking)
    // on LOS made with POI
    //  if LOS on target
    //   -> Engage
    //  else (no target)
    //   -> return to roam

    //Hide Usage
    // on the closest POI
    //  -> Find hiding spot
    // if has hiding spot
    //  -> Run to it
    #endregion



    #region Data Items
    //Environmental Awareness data
    [HideInInspector] public List<GameObject> POIs = new List<GameObject>();            // Footsteps                | Kept              | As the ability to run adds another layer of challenge to filtering out relevant POIs from own footsteps
    public PostDeathMemory.MemoryContents myMemory { get => PostDeathMemory.Instance[mechSystem.ID]; } //this is a relic from debugging, thats staying around for possible usage
    public List<CrateState> KnownCrates
    {
        get => myMemory.crateMemory;
        set
        {
            PostDeathMemory.MemoryContents mc = myMemory;
            mc.crateMemory = value;
            PostDeathMemory.Instance[mechSystem.ID] = mc; //not strictly needed since the struct means its reference based and the line above should change it in memory as well
        }
    }


    public CrateState getCrate() //Reconsider
    {
        //if (CrateGoal != null) //Removed so the bot could change crate target when wandering around in realtime
        //    return CrateGoal;

        //sort by distance to self
        KnownCrates.Sort((a, b) => (a.Position - transform.position).sqrMagnitude.CompareTo((b.Position - transform.position).sqrMagnitude));
        CrateState closestWas = null; //A crate that was there last we saw
        CrateState closestValidShould = null; //A crate that was gone last we saw, & should be back by now

        if(KnownCrates.Count == 1)
        {
            CrateGoal = KnownCrates[0];
            return CrateGoal;
        }

        foreach(var crate in KnownCrates)
        {
            if (crate.wasThere & closestWas == null)
            {
                closestWas = crate;
                continue;
            }

            if(!crate.wasThere && crate.shouldBeThere && closestValidShould == null)
            {
                closestValidShould = crate;
                continue;
            }
        }

        if(closestValidShould == null && closestWas != null) //Only 1 that was there was found
        {
            CrateGoal = closestWas;
            return CrateGoal;
        }

        if(closestWas == null && closestValidShould != null) //only 1 that should be there was found
        {
            CrateGoal = closestValidShould;
            return CrateGoal;
        }

        if(closestWas == null && closestValidShould == null) //IDK how the fck this would happen
        {
            CrateGoal = null;
            return null;
        }

        //both were available, pick the closest
        if (Vector3.Distance(closestWas.Position, transform.position) < Vector3.Distance(closestValidShould.Position, transform.position))
            CrateGoal = closestWas;
        else
            CrateGoal = closestValidShould;
        return CrateGoal;
    }
    CrateState CrateGoal = null;


    //contains an extra null test in case some how all POI's are null
    Vector3? currentPOI { get
        {
            List<Vector3> valids = new List<Vector3>();
            foreach (GameObject poi in POIs)
                if (poi != null)
                    valids.Add(poi.transform.position);

            try
            {
                valids.Sort((a, b) => (a - transform.position).sqrMagnitude.CompareTo((b - transform.position).sqrMagnitude));
            }
            catch (Exception e)
            {
                //rare case a POI may be destroyed between events. In this case we just return null
                return null;
            }
            
            for(int i = 0; i < valids.Count; i++) //just in case
                if (valids[i] != null)
                    return valids[i];
            return null;
        }}

    
    public float AuditoryRange = 25;
    public float AuditoryExclusionRange = 2;
    List<GameObject> ourFootsteps = new List<GameObject>();





    //Combat data
    Dictionary<int, CombatantInfo> ActiveCombatants = new Dictionary<int, CombatantInfo>(); // Combatants     | Kept      | Int refers to the Bot id, uniquely assigned on spawn for the leader board.
    [Task] void updateCombatants()
    {
        //This removes any combatants that have been destroyed during the fight that had fired at us
        if (ActiveCombatants.Count <= 0)
            return;

        Dictionary<int, CombatantInfo> replacement = new Dictionary<int, CombatantInfo>();
        foreach (KeyValuePair<int, CombatantInfo> mech in ActiveCombatants) //Removes destroyed bots & timed out combat bots & out of sight bots
        {
            if (mech.Value.TimeSinceCombat + combatReset < Time.time)
                continue;

            if (mech.Value.combatant == null)
                continue;

            if (!mechAIAiming.LineOfSight(mech.Value.combatant))
                continue;

            replacement.Add(mech.Key, mech.Value);
        }
        ActiveCombatants = replacement;
    }


    GameObject getCombatant
    {
        get
        {
            updateCombatants();
            //Return the first alive combatant in LOS
            foreach (var combatant in ActiveCombatants)
            {
                if (!combatant.Value.combatant) //just incase
                    continue;

                //mechAIAiming.aimTarget = combatant.Value.combatant.transform.GetChild(0).gameObject;
                return combatant.Value.combatant;
            }
            return null;
        }
    }
    float combatReset = 10;

    #region Sprinting Dropped
    public bool isSprinting = false; // Scraped | Well shit. This would have been an amazing thing to have since so many AI's in games can run, but aight i guess
    void setSprinting(bool state)
    {
        isSprinting = state;
        if (isSprinting)
        {
            mechAIMovement.agent.speed = 15;
            anim.SetBool("Running", true);
        }
        else
        {
            mechAIMovement.agent.speed = 5;
            anim.SetBool("Running", false);
        }
    }
    #endregion


    #endregion



    #region Aim - Observation system
    [Task] void findPOI()
    {
        //putting SFX on their own tag, giving them a trigger collider so they can be found using SphereOverlap, or having a common list (i would assume a hashset) of them for easier
        //implementation of shit like this would have been appreciated


        #region original attempt [incomplete] | was designed for sprinting allowance mostly, but sprinting aint allowed so go to the bottom
        /*
        Had some ideas on how to handle this. 
        Solution 1. Linking footsteps as a set of vectors that describe the path and expected next step as a prediction of position
                    Issues: 
                    1. if 2 bots were too close, there is a chance when finding steps to link, 
                       they could accidentally link to each others previous steps for the remaining duration.
                       Could happen even if SFXKill had an age variable to indicate time based for linking order.
                    2. No way to determine linking direction. If 3 points are in range, there is no value to distinguish direction, 
                       so the prediction point could be backwards. To correct this you would need to identify what point expired first

        Solution 2. Reducing the POI's to a list of single points using the bot closest to them as the value
                    Issues:
                    1. Provides no way to predict the path of another bot
                    2. Footsteps have a short lifespan, so its possible if the bot stops moving, there is no direct way to determine if
                       its because the bot is dead or because it has decided to camp a corner.
        
        Issue for both:
                    1. Inability to effectively remove own footsteps from the cycle without extra logic
                    possible solution: If the bot notes down is path in a set increment of distance covered, you could map that path in 2d, 
                                       and use that to exclude all footsteps made by yourself
        
        Solution 2 reduces the amount of data stored. Either way however, the process remains the same without an effective way of filtering 
        footsteps effectively.
        Solution 2 also has an added benefit of bypassing issue 2 by identifying previously marked spots in relation to the bot its related to.

        Eg.
        -> Footstep heard from Bot 1
           -> Position of the step and the bot number is marked
        -> new footstep heard from bot 1
           -> replace existing marked position
        -> no footstep is heard from bot 1 & point is still in range
           -> keep the POI marked
        -> cleanup
           -> remove all POI's outside auditory range

        Due to the possibility of something breaking a lot with the complexity of this system, the order of identification should be dependant on sorting by
        distance from bot to footstep so we get more accurate relations (hopefully)
        */

        ////all bots are tagged as Player, and im hoping it stays that way
        //GameObject[] bots = GameObject.FindGameObjectsWithTag("Player");    // All bots (including self)
        //List<GameObject> footSteps = new List<GameObject>();                // all footsteps in range
        //Dictionary<int, Vector3> finals = new Dictionary<int, Vector3>();   //This is so we can identify overlaps in points

        ////Filter out footsteps in audio range
        //foreach (GameObject gobj in GameObject.FindObjectsOfType<GameObject>())
        //{
        //    if (gobj.name.StartsWith("Footstep") && Vector3.Distance(transform.position, gobj.transform.position) <= AuditoryRange)
        //        footSteps.Add(gobj);
        //}

        //foreach (GameObject mech in bots)
        //{

        //}


        #endregion

        #region Shortchange version | This is the current path version that tracks where the bot has been to exclude foot steps
        //List<GameObject> footSteps = new List<GameObject>();                // all footsteps in range

        ////Filter out footsteps in audio range
        //foreach (GameObject gobj in GameObject.FindObjectsOfType<GameObject>())
        //{
        //    if (Vector3.Distance(transform.position, gobj.transform.position) > AuditoryRange)
        //        continue;

        //    if (!gobj.name.StartsWith("Footstep"))
        //        continue;

        //    foreach(Vector2 ourPath in currentPath)
        //    {
        //        Vector2 gobj2d = new Vector2(gobj.transform.position.x, gobj.transform.position.z);
        //        if(Vector2.Distance(gobj2d, new Vector2(transform.position.x, transform.position.z)) >= POI_Self_Exclusion_radius)
        //            footSteps.Add(gobj);
        //    }

        //}
        #endregion

        #region Lazy way | Jacks method
        List<GameObject> footSteps = new List<GameObject>();                // all footsteps in range

        //Filter out footsteps in audio range & exclusion range
        foreach (GameObject gobj in GameObject.FindObjectsOfType<GameObject>())
        {
            if (!gobj.name.StartsWith("Footstep"))
                continue;

            if (Vector3.Distance(transform.position, gobj.transform.position) > AuditoryRange)
                continue;

            if (Vector3.Distance(transform.position, gobj.transform.position) <= AuditoryExclusionRange)
            {
                ourFootsteps.Add(gobj);
                continue;
            }

            bool isOurs = false;
            foreach(GameObject ours in ourFootsteps)
            {
                if (ours == null)
                    continue;

                if(Vector3.Distance(ours.transform.position, gobj.transform.position) <= 0.2f)
                {
                    isOurs = true;
                    break;
                }

            }

            if (isOurs)
                continue;

            footSteps.Add(gobj);
        }

        //Do cleanup before adding and sorting
        foreach(GameObject poi in POIs)
            if (poi != null)
                footSteps.Add(poi); //Exclude any points that have been destroyed since the last rotation
        POIs = footSteps;

        List<GameObject> ours2 = new List<GameObject>();
        foreach (GameObject ours in ourFootsteps)
        {
            if (ours != null)
                ours2.Add(ours);
        }
        ourFootsteps = ours2;
        #endregion
    }

    //This will basically set on load of the bot
    int maxCrates
    {
        get => PostDeathMemory.Instance[mechSystem.ID].maxCrateCount;
        set
        {
            PostDeathMemory.MemoryContents mc = PostDeathMemory.Instance[mechSystem.ID];
            mc.maxCrateCount = value;
            PostDeathMemory.Instance[mechSystem.ID] = mc;
        }
    }
    [Task] void checkForCrate()
    {
        if (maxCrates == KnownCrates.Count & maxCrates != 0)//first frame, both will be zero, updateCrates has a return for if its zero tho
        {
            updateCrates();
            return; //No longer need to scan for new crates as all crates in the world have been found and marked
                    //The above has the added bonus of being a part of the bots memory, so afer enough time, it should eventually have all the crates available
        }
        else if (KnownCrates.Count > 0)
            updateCrates();


        int foundCrates = 0;
        List<Vector3> unkownCrates = new List<Vector3>(); //All crates in the scene rn
        foreach (GameObject gobj in GameObject.FindObjectsOfType<GameObject>())
        {
            if (!gobj.name.StartsWith("Mech Pickup"))
                continue;

            foundCrates++;
            bool isNew = true;
            foreach (CrateState cs in KnownCrates)
            {
                if (cs.ComparePoint(gobj.transform.position))
                {
                    isNew = false;
                    break;
                }
            }

            if (!isNew) //If we already know about it
                continue;

            unkownCrates.Add(gobj.transform.position);
        }
        if(foundCrates > maxCrates)
            maxCrates = foundCrates;

        //Check all unkown crates for LOS
        foreach(Vector3 cratePos in unkownCrates)
        {
            bool inView = checkInView(cratePos, (rayhit) =>
            {
                if (!rayhit.collider.gameObject.name.StartsWith("Mech Pickup"))
                    //We hit something other than the pickup
                    return false;

                //we hit the pickup
                return true;
            });

            //This makes it possible for the bot to miss a crate if its not respawned yet. Just like a player learning where they spawn
            if (inView)
                //Crate is in view and in line of sight = note its state
                KnownCrates.Add(new CrateState()
                {
                    Position = cratePos,
                    TimeLastValidated = Time.time,
                    wasThere = true
                });
        }
    }

    void updateCrates()
    {
        //Moved contents from end of checkForCrate to here during attempt to reduce operations/frame


        //Check all known crates for LOS testing
        for (int Index = 0; Index < KnownCrates.Count; Index++)
        {
            CrateState cs = KnownCrates[Index];

            if (!checkIsBarycentric(cs.Position))
                continue;


            RaycastHit rhit = new RaycastHit() { point = new Vector3(-32000, -32000, -32000) }; //dont use vector3.inf generally cause i've had a bad experience with it causeing issues in the past so this can do
            bool inView = checkInView(cs.Position, (rayhit) =>
            {
                rhit = rayhit;
                if (rayhit.collider.gameObject.name.StartsWith("Mech Pickup"))
                    //We hit the box
                    return true;

                //box wasn't there
                return false;
            });



            if (inView)
            {
                //Update the time and state of the crate
                KnownCrates[Index].wasThere = true;
                KnownCrates[Index].TimeLastValidated = Time.time;
            }
            else if(Vector3.Distance(rhit.point, cs.Position) <= 0.5) //we hit near the crate at least to confirm and not just hit a wall and say its not there
            {
                //It wasn't there
                KnownCrates[Index].wasThere = false;
            }
        }
    }
    #endregion


    #region Movement Tree
    //POI based
    [Task] private void Investigate_()
    {
        //If combat hueristic was green
        //-> Approach the POI and look for an enemy


        //Get all footsteps in the scene
        //and filter out by radius
        //they have no tag or layer so stuck ignoring only ones real close

        if (currentPOI == null)
            return;

        mechAIMovement.Movement((Vector3)currentPOI, 1);
    }

    private void Hide_() //cut due to giving up bc the constraints
    {
        //If combat heuristic was red
        //-> Find a hiding spot that is out of view of all POI's Predicted LOS (This should preference hiding near a crate)
        //   Sprint for this because even if the AI's pursue the sound, keeping away from them is preferable
    }

    //Default roam movement
    [Task] private void Roam_()
    {
        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f)
        {
            patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        }
        else
        {
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        }

        mechAIAiming.RandomAimTarget(patrolPoints);
    }
    
    [Task] private void Flee_()
    {
        //Dont expect this to run very frequently nor to work well since there is no layers or anyway to exclude the bots from the scanning
        //If getting duo'd
        //-> Go into Hiding. Avoid other POI's and try to get a crate while being pursued 

        //If there is an attack target, set it as the aimTarget 
        if (getCombatant)
        {
            //Child object correction - wonky pivot point
            mechAIAiming.aimTarget = getCombatant.transform.GetChild(0).gameObject;
        }
        else
        {
            //Look at random patrol points
            mechAIAiming.RandomAimTarget(patrolPoints);
        }

        //Move towards random patrol points <<< This could be drastically improved! - [Thomas] | Yeh, just not by me. Its sunday, I'm done tip toeing through this mine field of code trying to do something cool
        if (Vector3.Distance(transform.position, patrolPoints[patrolIndex].transform.position) <= 2.0f)
        {
            patrolIndex = Random.Range(0, patrolPoints.Length - 1);
        }
        else
        {
            mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 1);
        }
    }

    float lastKnownHealth = 0;
    [Task] private void Restock_()
    {

        CrateState targetPoint = getCrate();
        if (targetPoint == null)//there is a small instance between selecting a new crate and the crate spawning where this will be null
            return;
        mechAIMovement.Movement(targetPoint.Position, 0.5f);
        
        if(!inCombat())
            mechAIAiming.RandomAimTarget(patrolPoints); //look around while going to restock since we're not in combat

        if (mechSystem.health >= lastKnownHealth + 50)
        {
            for(int i = 0; i < KnownCrates.Count; i++)
            {
                if (targetPoint.ComparePoint(KnownCrates[i].Position))
                {
                    KnownCrates[i].wasThere = false;
                    KnownCrates[i].TimeLastValidated = Time.time;
                    CrateGoal = null;
                    lastKnownHealth = mechSystem.health;
                    return;
                }
            }
        }
        lastKnownHealth = mechSystem.health;
    }
    #endregion

    #region Attacking Tree
    [Task] private void Engage_() //Starts an engagement
    {
        if (!inCombat())
        {
            foreach (GameObject mech in mechAIAiming.targets)
            {
                if (checkInView(mech, (rhit) => rhit.collider.name.StartsWith("Battle Mech")))
                {
                    if (ActiveCombatants.ContainsKey(mech.GetComponent<MechSystems>().ID))
                    {
                        CombatantInfo ci = ActiveCombatants[mech.GetComponent<MechSystems>().ID];
                        ci.TimeSinceCombat = Time.time;
                        ActiveCombatants[mech.GetComponent<MechSystems>().ID] = ci;
                        break;
                    }

                    ActiveCombatants.Add(mech.GetComponent<MechSystems>().ID, new CombatantInfo()
                    {
                        combatant = mech,
                        TimeSinceCombat = Time.time
                    });
                    break;
                }
            }
        }
    }

    //Keeps you in range of the combat
    public float StrafeRange = 20;
    [Task] void Fight_()
    {
        GameObject t = getCombatant;

        if (t)
        {
            mechAIAiming.aimTarget = t.transform.GetChild(0).gameObject; //For some fucked up reason, mine will still aim at the ground despite having this
            //Child object correction - wonky pivot point


            //Move Towards attack Target
            if (Vector3.Distance(transform.position, t.transform.position) >= 45.0f)
            {
                mechAIMovement.Movement(t.transform.position, 45);
                return;
            }

            //Otherwise "strafe" - move towards random patrol points at intervals
            else if (Vector3.Distance(transform.position, t.transform.position) < 30.0f && Time.time > attackTimer)
            {
                //patrolIndex = Random.Range(0, patrolPoints.Length - 1);
                //mechAIMovement.Movement(patrolPoints[patrolIndex].transform.position, 2);
                //attackTimer = Time.time + attackTime + Random.Range(-0.5f, 0.5f);

                //The bot would prioritize moving to the patrol point over staying in the combat case
                float X = StrafeRange * Random.value; 
                float Z = StrafeRange * Random.value;
                mechAIMovement.Movement(new Vector3(X, transform.position.y, Z), 0.5f);
                attackTimer = Time.time + attackTime + Random.Range(-0.5f, 0.5f);
            }
        }
    }
    #endregion

    
    public override void TakingFire(int origin)
    {
        //Self damage
        if (origin == mechSystem.ID)
            return;

        //The target is already known
        if (ActiveCombatants.ContainsKey(origin))
        {
            //Update its last combat time
            CombatantInfo ci = ActiveCombatants[origin];
            ci.TimeSinceCombat = Time.time;
            ActiveCombatants[origin] = ci;
            return;
        }
            


        foreach (GameObject target in mechAIAiming.targets)
        {
            if (!target) //target is dead
                continue;

            if (origin == target.GetComponent<MechSystems>().ID)
                ActiveCombatants.Add(origin, new CombatantInfo()
                {
                    TimeSinceCombat = Time.time,
                    combatant = target
                });
        }
    }


    #region Weapons & conditions
    float maxEnergy = 0;

    void checkAimTarget(GameObject t) => mechAIAiming.aimTarget ??= t;

    [Task]
    bool canBeFired_bl()
    {
        GameObject t = getCombatant;
        if (t == null)
            return false;
        checkAimTarget(t);

        return Vector3.Distance(transform.position, t.transform.position) > 17.5 && //reduced the minimum range a little
        Vector3.Distance(transform.position, t.transform.position) < 50 &&
        mechSystem.energy > (maxEnergy * 0.2) &&
        mechAIAiming.FireAngle(20f)
        ; //Tightened the angle
    }

    [Task]
    void FirinMaLaser_start() => //big laser
        mechAIWeapons.laserBeamAI = true;

    [Task]
    void FirinMaLaser_stop() => //big laser
        mechAIWeapons.laserBeamAI = false;



    [Task]
    bool canBeFired_l()
    {

        GameObject t = getCombatant;
        if (t == null)
            return false;
        checkAimTarget(t);
        //Little laser
        return mechSystem.energy > (maxEnergy * 0.05) && //More than 5% of max energy
        mechAIAiming.FireAngle(25)
        ; //Increased the angle
    }
    
    [Task]
    void fireLaser() => //little laser
        mechAIWeapons.Lasers();



    [Task]
    bool canBeFired_s()
    {
        GameObject t = getCombatant;
        if (t == null)
            return false;
        checkAimTarget(t);

        return Vector3.Distance(transform.position, getCombatant.transform.position) > 25 &&
        mechSystem.shells > 6 && //6 = 3 shots
        mechAIAiming.FireAngle(10)
        ; //Tightened the angle
    }

    [Task]
    void fireShells() => //shells
        mechAIWeapons.Cannons();



    [Task] //Missiles
    bool canBeFired_m() {

        GameObject t = getCombatant;
        if(t == null)
            return false;
        checkAimTarget(t);

        return
        Vector3.Distance(transform.position, t.transform.position) > 50 &&
        mechSystem.missiles >= 18 &&
        mechAIAiming.FireAngle(5)
        ;
    }
    
    [Task]
    void fireMissiles() => //Missiles
        mechAIWeapons.MissileArray();
    #endregion


    #region Conditions
    float maxHealth = 0;
    [Task] public bool combatReady() => mechSystem.health > (maxHealth * 0.4) && mechSystem.energy > (maxEnergy * 0.10) && (mechSystem.missiles >= 20 | mechSystem.shells >= 8);
    [Task] public bool needCombatRestock() => (mechSystem.health <= (maxHealth * 0.6) && mechSystem.energy <= (maxEnergy * 0.3)) || (mechSystem.missiles < 20 | mechSystem.shells < 8);

    
    [Task] public bool inCombat() => ActiveCombatants.Count > 0;
    [Task] public bool tagTeamed() => ActiveCombatants.Count >= 2; //if being shot at by 2 or more people

    [Task] public bool hasPOI => POIs.Count > 0 && currentPOI != null; //This will auto sort to the closest & test if its a valid point

    [Task] bool foundCrates() => KnownCrates.Count > 0;
    #endregion


    #region Other
    public void debugTargetData()
    {
        string print = "Targets: ";
        foreach (GameObject obj in mechAIAiming.targets)
            print += $"\n{obj.name} | Id: {obj.GetComponent<MechSystems>().ID}";
        Debug.Log(print);

        print = "Current Targets: ";
        foreach (GameObject obj in mechAIAiming.currentTargets)
            print += $"\n{obj.name} | Id: {obj.GetComponent<MechSystems>().ID}";
        Debug.Log(print);
    }


    //This class is so the bot can retain some info after dying rather than having amnesia
    public class PostDeathMemory
    {
        public static PostDeathMemory Instance
        {
            get
            {
                if(Instance_ == null)
                    Instance_ = new PostDeathMemory();
                return Instance_;
            }
        }
        static PostDeathMemory Instance_ = null;


        Dictionary<int, MemoryContents> droneAccess = new Dictionary<int, MemoryContents>();
        public struct MemoryContents
        {
            public List<CrateState> crateMemory;
            public int maxCrateCount;
        }

        public MemoryContents this[int droneID]
        {
            get
            {
                if (!Instance.droneAccess.ContainsKey(droneID))
                {
                    //dont really care if the id refers to an actual drone or not, its more for if there are multiple drones of the same AI in the scene
                    Instance.droneAccess[droneID] = new MemoryContents()
                    {
                        crateMemory = new List<CrateState>(),
                        maxCrateCount = 0
                    };
                    Debug.Log($"Memory entry for drone {droneID} made");
                }
                return Instance.droneAccess[droneID];
            }
            set
            {
                Instance.droneAccess[droneID] = value;
            }
        }

        public int getCount() => Instance.droneAccess.Count;
        public bool hasKey(int Key) => Instance.droneAccess.ContainsKey(Key);
    }


    struct CombatantInfo
    {
        public GameObject combatant;
        public float TimeSinceCombat;
    }
    #endregion
}





[CustomEditor(typeof(MechAIDecisions_Thomas))]
class EditorInformation : Editor
{
    private void OnSceneGUI()
    {
        MechAIDecisions_Thomas mai = target as MechAIDecisions_Thomas;
        Handles.DrawWireArc(mai.transform.position, Vector3.up, mai.transform.forward, 360, mai.AuditoryRange);
        Handles.DrawWireArc(mai.transform.position, Vector3.up, mai.transform.forward, 360, mai.AuditoryExclusionRange);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        MechAIDecisions_Thomas mai = target as MechAIDecisions_Thomas;

        if (GUILayout.Button("Print Aiming Target data"))
            mai.debugTargetData();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Gloabal Memory Entries: ");
        GUILayout.Label(MechAIDecisions_Thomas.PostDeathMemory.Instance.getCount().ToString());
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Has Memory: {(MechAIDecisions_Thomas.PostDeathMemory.Instance.hasKey(mai.GetComponent<MechSystems>().ID) ? "True" : "False")}");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Known Crates: {MechAIDecisions_Thomas.PostDeathMemory.Instance[mai.GetComponent<MechSystems>().ID].crateMemory.Count}");
        GUILayout.Label($"Known Max: {MechAIDecisions_Thomas.PostDeathMemory.Instance[mai.GetComponent<MechSystems>().ID].maxCrateCount}");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Combat Ready: {(mai.combatReady() ? "True" : "False")}");
        GUILayout.Label($"Combat Restock: {(mai.needCombatRestock() ? "True" : "False")}");
        GUILayout.Label($"inCombat: {(mai.inCombat() ? "True" : "False")}");
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        CrateState cs = mai.getCrate();
        GUILayout.Label($"Target crate: {(cs != null ? cs.Position.ToString() : "null")}");
        GUILayout.EndHorizontal();
    }
}



//Crates are made and removed not active and inactive. so comparing by ID is a horrible idea
public class CrateState : IComparable<CrateState>
{
    public Vector3 Position; //The related crates known position
    public float TimeLastValidated;
    public bool wasThere;
    public bool shouldBeThere => TimeLastValidated + 5 < Time.time; //5 is the respawn time on the crates internally. this way we have a "feel" if the crate is back or not


    //off chance a crate doesn't spawn in the exact same spot. Also since vectors are structure they have no inherent CompareTo, which would still be bad if they did since 1 * 10^-10 could cause a false comparison
    public bool ComparePoint(Vector3 CompareAgainst) => Vector3.Distance(Position, CompareAgainst) <= 0.5f;

    public int CompareTo(CrateState other) => Position.ToString().CompareTo(other.Position.ToString());
}
