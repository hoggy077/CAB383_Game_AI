using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Mothership : Enemy {

    GameManager gmM;

    public GameObject enemyPrefab;
    public int numberOfEnemies = 20;

    public GameObject spawnLocation;

    bool runOnce = true;
    bool onStarted = true;


    public GameObject BeamMuzzle;
    public GameObject BeamTarget;
    public LineRenderer beam;
    private float beamFireRate = 3;
    private float beamFireTime;
    private float beamFireDuration = 1.5f;

    public float shield = 5000;
    public GameObject shieldObj;


    public override void takeDamage(float dmg)
    {
        if(shield > dmg)
        {
            shield -= dmg;
            dmg = 0;
        }
        else if (shield <= dmg)
        {
            dmg -= shield;
            shield = 0;
            shieldObj.SetActive(false);
        }

        base.takeDamage(dmg);

    }


    void fireLazer()
    {
        RaycastHit hit;
        if (beamFireTime >= beamFireRate + beamFireDuration && Physics.Raycast(BeamMuzzle.transform.position, -(BeamMuzzle.transform.position - gmM.playerDreadnaught.transform.position).normalized, out hit, 1000.0f))
        {
            BeamMuzzle.transform.LookAt(gmM.playerDreadnaught.transform.position);
            beam.enabled = true;
            beam.positionCount = 2;
            beam.SetPosition(0, BeamMuzzle.transform.position);
            beam.SetPosition(1, BeamTarget.transform.position);
            beam.material.SetTextureOffset("_MainTex", new Vector2(-Time.time * 3, 0.0f));

            if (!GetComponent<AudioSource>().isPlaying)
                GetComponent<AudioSource>().Play();

            if (hit.transform.tag == "Player")
            {
                beam.SetPosition(1, hit.transform.position);
            }
            else
            {
                beam.enabled = false;
                GetComponent<AudioSource>().Stop();
            }
            beamFireTime = 0;
        }
        else
            beam.enabled = false;
        beamFireTime += Time.deltaTime;
    }


    void Start() {

        for (int i = 0; i < numberOfEnemies; i++) {

            Vector3 spawnPosition = spawnLocation.transform.position;

            spawnPosition.x = spawnPosition.x + Random.Range(-50, 50);
            spawnPosition.y = spawnPosition.y + Random.Range(-50, 50);
            spawnPosition.z = spawnPosition.z + Random.Range(-50, 50);

            GameObject DroneObj = Instantiate(enemyPrefab, spawnPosition, spawnLocation.transform.rotation);
            Live_Drones.Add(DroneObj);
            DroneStates.Add(DroneObj.GetComponent<Drone>(), false);
        }
    }

    int lastList = 0;
    float timer = 20;
    float time = 0;

    void Update() {

        if (runOnce)
        {
            findBestDrones();
            gmM = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
            runOnce = false;
        }

        if (gmM.gameStarted)
            fireLazer();


        //collection updated, review the stuffs
        if (lastList != Resource_Collection.Count && time >= timer)
        {
            lastList = Resource_Collection.Count;
            SortAsteroids();

            if (Resource_Collection.Count >= 5)
                Assign_Foraging();
        }
        else
            time += Time.deltaTime;

        if (gmM.gameStarted && onStarted)
        {
            Establish_combat_Group();

            onStarted = false;
        }
    }

    #region Drone Management
    [Header("Drone Management")]
    public List<GameObject> Live_Drones = new List<GameObject>();
    private Dictionary<Drone, bool> DroneStates = new Dictionary<Drone, bool>();    //False = free to use, true = actively assigned a job. This only applies to scouts and elites as they should retain their job
    public List<GameObject> Scout_Drones = new List<GameObject>();                  //Since the heuristic doesn't care about the temporary values these never change
    public List<GameObject> Elite_Drones = new List<GameObject>();
    private int remainingJobless
    {
        get
        {
            return numberOfEnemies - (Scout_Drones.Count + Elite_Drones.Count);
        }
    }

    public int MaxScouts = 4;
    public int MaxElites = 2;

    void findBestDrones()
    {
        //allowing an additional temporary entry as this lets the next drone in the search to be sorted in
        List<Drone> bestScouts = new List<Drone>();
        List<Drone> bestElites = new List<Drone>();


        foreach(GameObject obj in Live_Drones)
        {
            Drone THE_Drone = obj.GetComponent<Drone>();
            bestScouts.Add(THE_Drone);
            bestElites.Add(THE_Drone);
        }

        bestElites.Sort((a, b) => a.individualInfo.get_Heuristic().CompareTo(b.individualInfo.get_Heuristic()));
        bestScouts.Sort((a, b) => b.individualInfo.get_Heuristic().CompareTo(a.individualInfo.get_Heuristic()));

        for (int ind = 0; ind < MaxScouts; ind++)
        {
            //bestScouts[ind].Group_ID = -1;
            bestScouts[ind].State = Drone.BehaviourState.Scouting;
            bestScouts[ind].gameObject.name += " Scout";

            Scout_Drones.Add(bestScouts[ind].gameObject);
            DroneStates[bestScouts[ind]] = true;
        }

        for (int ind = 0; ind < MaxElites; ind++)
        {
            bestElites[ind].gameObject.name += " Elite";
            bestElites[ind].individualInfo.isElite = true;
            Elite_Drones.Add(bestElites[ind].gameObject);
            DroneStates[bestScouts[ind]] = true;
        }
    }
    


    public void InitNextScout()
    {
        if(Scout_Drones.Count < MaxScouts)
        {
            //find a new drone as a viable scout
            Drone final = null;
            foreach(Drone drn in DroneStates.Keys)
            {
                if (final == null)
                {
                    final = drn;
                    continue;
                }

                if (DroneStates[drn])
                    continue;

                if (drn.individualInfo.get_Heuristic() > final.individualInfo.get_Heuristic())
                    final = drn;
            }

            final.State = Drone.BehaviourState.Scouting;
            DroneStates[final] = true;
            Scout_Drones.Add(final.gameObject);
        }
    }

    public void ReleaseScout(Drone scout)
    {
        Scout_Drones.Remove(Scout_Drones.Find((drn) => drn == scout.gameObject));
        DroneStates[scout] = false;
    }

    public void release_inUse(Drone caller) => DroneStates[caller] = false;


    public void Assign_Foraging()
    {
        //Assign the elites

        //breakdown
        //We got X drones, with a minimum of 5 asteroids before we start foraging
        //only Z amount of drones tho can be assigned to 

        int ind = 0;
        foreach(GameObject obj in Elite_Drones)
        {
            //Assign all the elite drones to the top 2
            Drone drn = obj.GetComponent<Drone>();
            drn.State = Drone.BehaviourState.Foraging;
            drn.ForagingTarget = Resource_Collection[ind];
            ind++;
        }

        ind = MaxElites;
        foreach (GameObject obj in Live_Drones)
        {
            if (Elite_Drones.Contains(obj))
                continue;

            if (Scout_Drones.Contains(obj))
                continue;

            //pick a random resource from the remaining list
            //assign it, and set to forage
            Drone drn = obj.GetComponent<Drone>();
            drn.State = Drone.BehaviourState.Foraging;
            drn.ForagingTarget = Resource_Collection[ind];
            ind++;

            if (ind >= Resource_Collection.Count)
                ind = MaxElites;
        }
    }

    public int combatGroupMin = 5;
    public float groupChance = 0.2f;
    public float groupSizeCheck = 25;

    public LayerMask droneMask;

    private int groupID_Step = 1;

    public void Establish_combat_Group()
    {
        //reset all drone groups in case
        //foreach(GameObject obj in Live_Drones)
        //{
        //    Drone drn = obj.GetComponent<Drone>();
        //    drn.Group_ID = 0;
        //    drn.GroupLeader = null;
        //    drn.isGroupLeader = false;
        //}

        //form groups
        foreach(GameObject obj in Live_Drones)
        {
            Drone drn = obj.GetComponent<Drone>();
            if (drn.GroupLeader != null)
                continue;

            if (Random.value > groupChance)
                continue;


            Collider[] localdrones = Physics.OverlapSphere(obj.transform.position, groupSizeCheck, droneMask);

            if (localdrones.Length < groupSizeCheck)
                return;

            drn.Group_ID = groupID_Step;
            drn.isGroupLeader = true;

            foreach (Collider coll in localdrones)
            {
                Drone ldrn = coll.GetComponent<Drone>();
                ldrn.Group_ID = groupID_Step;
                ldrn.GroupLeader = drn;
            }

            groupID_Step++;
        }
    }

    #endregion

    #region Resource/Asteroids
    [Header("Resource Management")]
    public float SearchRadius = 500f;
    public List<Asteroid> Resource_Collection = new List<Asteroid>();


    public float ResourcesGained = 0;

    void SortAsteroids() => Resource_Collection.Sort((ast_a, ast_b) => ast_b.Resource_Count.CompareTo(ast_a.Resource_Count));

    public void receiveResource(float quantity) => ResourcesGained += quantity;


    public float perShrink = 50f;

    public void shrinkNeighborhood()
    {
        foreach(GameObject obj in Live_Drones)
            obj.GetComponent<Drone>().point_Radius -= perShrink;
    }

    public void evaluateCollection()
    {
        bool happened = false;
        List<Asteroid> removeList = new List<Asteroid>();
        foreach(Asteroid ast in Resource_Collection)
        {
            if (ast.deadCount >= 5)
            {
                //Resource_Collection.Remove(ast);
                removeList.Add(ast);
                happened = true;
            }
        }

        foreach(Asteroid ast in removeList)
            Resource_Collection.Remove(ast);

        if (happened)
        {
            shrinkNeighborhood();
            InitNextScout();

        }
    }
    #endregion

    public override void onDeath()
    {
        Live_Drones.Clear();
        Scout_Drones.Clear();
        Elite_Drones.Clear();
    }
}
