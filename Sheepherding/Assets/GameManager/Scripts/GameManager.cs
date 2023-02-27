using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using csDelaunay;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
  // GUI
  [Header("GUI")]
  public Text countdownText;
  public Text scoreText;

  // timer; time available to drive all sheep into the barn 
  //private float gameTimer = 150.0f;

  // game settings
  // hardcoded spawn boundaries
  private float minSpawnX = -50.0f;
  private float maxSpawnX = 50.0f;
  private float maxSpawnZ = 30.0f;
  private float minSpawnZ = -55.0f;

  // skybox
  [Header("Skybox")]
  public Material[] skyboxes;

  // fences
  [Header("Fence")]
  public GameObject fence;
  [Header("Barn doors")]
  public GameObject barnDoors;
  [HideInInspector]
  public Collider[] fenceColliders;

  // sheep
  [Header("Sheep")]
  public GameObject sheepPrefab;
  private float minSheepSize = .7f;  // random size
  public int nOfSheep = 100;
  [HideInInspector]
  public int sheepCount; // sheep remaining on field
  // list of sheep
  public List<SheepController> sheepList = new List<SheepController>();

  public float sheepMaxSpeedChange = .15f / .02f; // in deg per second
  public float sheepMaxTurn = 4.5f / .02f; // in deg per second

  public float sheepWalkingSpeed = 1.5f; // Strombom original 0.15f
  public float sheepRunningSpeed = 7.5f; // Strombom original 1.5f

  [System.Serializable]
	public class SPG
  {
    public float r_0 = 1.0f; // interaction distance
    public float r_e = 1.0f; // equilibrium distance for atraction/repulsion force
    public float eta = 0.13f; // noise - original 0.13f
    public float beta = 3.0f; // cohesion factor - original 0.8f
    public float alpha = 15.0f; // allelometric parameters
    public float delta = 4.0f; // allelometric parameters
    public float tau_iw = 35f; // transition rate, idle to walking - original 35
    public float tau_wi = 8f; // transition rate, walking to idle - original 8
    public float tau_iwr; // transition rate, idle, walking to running = sheepCount
    public float tau_ri; // transition rate, running to idle = sheepCount
    public float d_R = 31.6f; // lenghtscale for transitions to running
    public float d_S = 6.3f; // lenghtscale for transitions to idle

    public float rho_s = 3f; // shephard repulsion factor
    public float r_s = 22.5f; // shephard detection distance
    public float r_sS = 22.5f / 2f; // shephard strong repulsion distance

    public float rho_f = 2f; // fences repulsion factor
    public float r_f = 3f; // fence detection distance

    //public int n = 20; // cognitive limits
    public bool occlusion = false; // bahaviour based on local info only, i.e. visible non occluded Sheep
    public float blindAngle = 0f;
  }
  public SPG SheepParametersGinelli;
	// public int ns = 8; // experimental: interact with maximally ns neighbours (cognitive limit)

  public bool StrombomSheep = true;
  [System.Serializable]
  public class SPS
  { // distance are half of what is given in the paper as it seems they used sheeps of size 2, ours are of size 1 (diameter of circle)
    public float r_a = 1f; // agent to agent interaction distance - original 2f
    public float rho_a = 2f; // relative strentgth of repulsion from other agents
    public float rho_s = 1f; // relative strength of repulsion from the shepherd
    public float c = 1.05f; // relative strength of attraction to the n nearest neighbours
    public float h = 0.5f; // relative strength of proceeding in the previous direction
    public float e = 0.1f; // relative strength of angular noise - original 0.3f

    public float r_s = 22.5f; // shepherd detection distance - original 45f
    public float r_sS = 22.5f / 2f; // shepherd detection distance - strong repulsion (running otherwise walking)

    public float rho_f = 2f; // relative strength of repulsion from fences
    public float r_f = 3f; // fence detection distance

    public int n = 20; // > min(0.53sheepCount, 3log(sheepCount))
    public bool occlusion = false; // bahaviour based on local info only, i.e. visible non occluded Sheep
    public float blindAngle = 0f;
  }
  public SPS SheepParametersStrombom;
  [Header("Wolf")]
  // list of wolves
  public List<WolfController> wolfList = new List<WolfController>();

  public float wolfMaxSpeedChange = 62f;
  public float wolfMaxTurn = 360f; // in deg per second

  public float wolfMinSpeed = -3f;
  public float wolfWalkingSpeed = 1.5f; 
  public float wolfRunningSpeed = 7.5f; // Strombom original 1.5f
  public float wolfMaxSpeed = 10f;

  public bool BehaviourWolves = false; // use Strömbom et al.'s shepherd
  [System.Serializable]
  public class DPS
  {
    public float r_s = 3;// length at which wolf stops 3ro
    public float r_w = 9;// length at which wolf starts walking
    public float r_r = 18;// length at which wolf starts running

    public bool local = false; // use local model
    public int ns = 20; // size of local subgroups
    public bool occlusion = false; // bahaviour based on local info only, i.e. visible non occluded Sheep
    public float blindAngle = 60f; // in degrees
  }
  public DPS WolfsParametersStrombom;

	[System.Serializable]
	public class WA
	{
		// Attacking parameters
		public float attackDistance = 5.0f;
		public float eatingTime = 3f;
		public float focusTime = 2f;
    public Enums.AttackTactic at = Enums.AttackTactic.closest;
  }
	public WA WolfAttack;

  [System.Serializable]
  public class WBP
  {
    public float rc = 10.0f;
    public float ra = 7.0f;
    public float rs = 4.0f;

    public float separationFactor = 1.0f;
    public float allignmentFactor = 1.0f;
    public float cohesionFactor = 1.0f;
    public float gtFactor = 1.0f;
  }
  public WBP WolfsBoidParameters;

  [System.Serializable]
  public class WD
  {
    public float safeDistance = 5.0f; // distance at which wolf starts running away from an LGD
  }
  public WD WolfDefense;
  

  [Header("Guard Dog")]
  public List<GuardDogController> guardDogList = new List<GuardDogController>();
  public float dogMaxSpeedChange = 62f;
  public float dogMaxTurn = 360f; // in deg per second

  public float dogMinSpeed = -3f;
  public float dogWalkingSpeed = 1.5f;
  public float dogRunningSpeed = 7.5f; // Strombom original 1.5f
  public float dogMaxSpeed = 10f;

  [System.Serializable]
  public class LGDT
  {
    public float ra = 50f;
    public float rd = 20f;
    public float rr = 5f;
    public float blindAngle = 60f;
    public bool local = false;
    public int ns = 7;
    public bool occlusion = false;
    public float safeDistance = 3.0f;
    public float intersectDistance = 7.0f;
    public bool attackTactic = false;
    public float separationDistance = 4.0f;
  }
  public LGDT guardDogTactic;

  // collision avoidance parameters
  [System.Serializable]
	public class SCA
	{
		public bool SHEEPCOLLISION_ON = true;
		public float collisionSphereRadius = 0.7f;
		public float collisionAvoidDst = 4f;
		public float collisionAvoidWeight = 2f;
		public LayerMask obstacleMask;
	}
  [Header("Collision")]
  public SCA SheepCollisionAvoidance;

	[System.Serializable]
	public class DCA
	{
		public float collisionSphereRadius = 0.7f;
		public float collisionAvoidDst = 4f;
		public float collisionAvoidWeight = 2f;
		public LayerMask obstacleMask;
	}
	public DCA DogsCollisionAvoidance;

  // update frequency
  private float neighboursUpdateInterval = 0*.5f;
  private float neighboursTimer;

  void Start()
  {
    // spawn
    SpawnSheep();

    // fences colliders
    fenceColliders = fence.GetComponentsInChildren<Collider>().Concat(barnDoors.GetComponentsInChildren<Collider>()).ToArray();

    // timers
    neighboursTimer = neighboursUpdateInterval;
  }

  void SpawnSheep()
  {
    // number of sheep
    sheepCount = nOfSheep;
    SheepParametersGinelli.tau_iwr = nOfSheep;
    SheepParametersGinelli.tau_ri = nOfSheep;

    // cleanup
    int i = 0;
    sheepList.Clear();
    GameObject[] sheep = GameObject.FindGameObjectsWithTag("Sheep");
    for (i = 0; i < sheep.Length; i++)
      Destroy(sheep[i]);

    // spawn
    Vector3 position;
    SheepController newSheep;

    i = 0;
    while (i < sheepCount)
    {
      position = new Vector3(Random.Range(minSpawnX, maxSpawnX), .0f, Random.Range(minSpawnZ, maxSpawnZ));

      if (Physics.CheckSphere(position, 1.0f, 1 << 8)) // check if random position inside SheepSpawn areas
      {
        float randomFloat = Random.Range(minSheepSize, 1.0f);
        newSheep = ((GameObject)Instantiate(sheepPrefab, position, Quaternion.identity)).GetComponent<SheepController>();
        newSheep.id = i;
        newSheep.transform.localScale = new Vector3(randomFloat, randomFloat, randomFloat);
        sheepList.Add(newSheep);
        i++;
      }
    }
    // remove spawn areas
    foreach (GameObject area in GameObject.FindGameObjectsWithTag("SpawnArea"))
      GameObject.Destroy(area);

    // find wolves
    wolfList = new List<WolfController>(FindObjectsOfType<WolfController>());

    // find dogs
    guardDogList = new List<GuardDogController>(FindObjectsOfType<GuardDogController>());
  }

  public void Quit()
  {
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
      Application.Quit();
#endif
  }

  void Update()
  {
    // pause menu
    if (Input.GetKeyDown(KeyCode.Escape))
    {
      Quit();
    }

    // update
    UpdateNeighbours();
  }

  private void UpdateNeighbours()
  {
    neighboursTimer -= Time.deltaTime;
    if (neighboursTimer < 0)
    {
      neighboursTimer = neighboursUpdateInterval;

      if (!StrombomSheep)
      {
        // comment out to change via inspector
        SheepParametersGinelli.tau_iwr = sheepCount;
        SheepParametersGinelli.tau_ri = sheepCount;

        // todo test with occlusion, cognitive limit and both dogs and sheep in same perception then filter out and merge from T+M dogs 
        List<Vector2f> sheepL = new List<Vector2f>();
        List<Vector2f> wolfL = new List<Vector2f>();
        List<Vector2f> dogL = new List<Vector2f>();

        // recast position data for cache coherence
        foreach (SheepController sc in sheepList)
          if (!sc.dead) sheepL.Add(new Vector2f(sc.transform.position.x, sc.transform.position.z, sc.id));
        foreach (WolfController dc in wolfList)
          wolfL.Add(new Vector2f(dc.transform.position.x, dc.transform.position.z, dc.id)); // GM.nOfSheep * 10 + 
        foreach (GuardDogController lgd in guardDogList)
          dogL.Add(new Vector2f(lgd.transform.position.x, lgd.transform.position.z, lgd.id));

        // topologic neighbours - first shell of voronoi neighbours
        Rectf bounds = new Rectf(-60.0f, -65.0f, 120.0f, 110.0f);
        Voronoi voronoi = new Voronoi(sheepL, bounds);
#if !StrombomSheep
        Debug.DrawLine(new Vector3(bounds.x, 0, bounds.y), new Vector3(bounds.x + bounds.width, 0, bounds.y));
        Debug.DrawLine(new Vector3(bounds.x + bounds.width, 0, bounds.y), new Vector3(bounds.x + bounds.width, 0, bounds.y + bounds.height));
        Debug.DrawLine(new Vector3(bounds.x + bounds.width, 0, bounds.y + bounds.height), new Vector3(bounds.x, 0, bounds.y + bounds.height));
        Debug.DrawLine(new Vector3(bounds.x, 0, bounds.y + bounds.height), new Vector3(bounds.x, 0, bounds.y));
        foreach (LineSegment ls in voronoi.VoronoiDiagram())
          Debug.DrawLine(new Vector3(ls.p0.x, 0f, ls.p0.y), new Vector3(ls.p1.x, 0f, ls.p1.y), Color.black);
#endif

        foreach (SheepController sc in sheepList)
        {
          Vector2f position = new Vector2f(sc.transform.position.x, sc.transform.position.z, sc.id);

          // get metric wolf neighbours
          List<WolfController> wolfNeighbours = new List<WolfController>();
          var wolfs = wolfL.Where(point => point.DistanceSquare(position) < SheepParametersGinelli.r_s * SheepParametersGinelli.r_s);
          wolfs.OrderBy(d => d, new ByDistanceFrom(position));
          //    .Take(SheepParametersGinelli.n); // cognitive limits
          foreach (Vector2f dn in wolfs)
            wolfNeighbours.Add(wolfList[dn.id]);

          // get metric wolf neighbours
          List<GuardDogController> dogNeighbours = new List<GuardDogController>();
          var dogs = dogL.Where(point => point.DistanceSquare(position) < SheepParametersGinelli.r_s * SheepParametersGinelli.r_s);
          dogs.OrderBy(d => d, new ByDistanceFrom(position));
          //    .Take(SheepParametersGinelli.n); // cognitive limits
          foreach (Vector2f dn in dogs)
            dogNeighbours.Add(guardDogList[dn.id]);

          // get topologic sheep neighbours
          List<SheepController> topologicNeighbours = new List<SheepController>();
          foreach (Vector2f snt in voronoi.NeighborSitesForSite(position))
            topologicNeighbours.Add(sheepList[snt.id]);

          // get metric sheep neighbours
          List<SheepController> metricNeighbours = new List<SheepController>();
          var sheep = sheepL.Where(point => point.id != sc.id && point.DistanceSquare(position) < SheepParametersGinelli.r_0 * SheepParametersGinelli.r_0);
          sheep.OrderBy(s => s, new ByDistanceFrom(position));
          //     .Take(SheepParametersGinelli.n); // cognitive limits
          foreach (Vector2f snm in sheep)
            metricNeighbours.Add(sheepList[snm.id]);

          // perform updates by swap to prevent empty lists due to asynchronous execution
          sc.wolfNeighbours = wolfNeighbours;
          sc.dogNeighbours = guardDogList;
          sc.topologicNeighbours = topologicNeighbours;
          sc.metricNeighbours = metricNeighbours;

          // ignore topologic neighbours further than the closest dog
//          float l_i = sc.l_i;
#if false
          if (dogs.Count > 0)
          {
            float ndc = (dogList[dogs[0].id].transform.position - sc.transform.position).magnitude;
            topologicNeighbours =
              topologicNeighbours.Where(snt => (snt.transform.position - sc.transform.position).magnitude < ndc).ToList(); 
            //!(snt.sheepState == Enums.SheepState.idle && snt.previousSheepState == Enums.SheepState.running)
            sc.topologicNeighbours = topologicNeighbours;
          }
#endif
          // TODO: metricNeighbours.Where(n => !n.dead && n.sheepState == Enums.SheepState.idle).Count();
          sc.n_idle = .0f;
          sc.n_walking = .0f;
          sc.m_idle = .0f;
          sc.m_toidle = .0f;
          sc.m_running = .0f;
          sc.l_i = .0f;
          // ignore dead/barned sheep
          foreach (SheepController neighbour in sc.metricNeighbours)
          {
            if (neighbour.dead) continue;
            // state counter
            switch (neighbour.sheepState)
            {
              case Enums.SheepState.idle:
                sc.n_idle++;
                break;
              case Enums.SheepState.walking:
                sc.n_walking++;
                break;
            }
          }

          // ignore dead/barned sheep
          foreach (SheepController neighbour in sc.topologicNeighbours)
          {
            if (neighbour.dead) continue;
            // state count
            switch (neighbour.sheepState)
            {
              case Enums.SheepState.idle:
                if (neighbour.previousSheepState == Enums.SheepState.running)
                  sc.m_toidle++;
                  //sc.m_toidle += 1f - Mathf.Max(0, (neighbour.transform.position - sc.transform.position).magnitude / l_i); // decrease influence of idle sheep with their distance
                sc.m_idle++;
                break;
              case Enums.SheepState.running:
                sc.m_running++;
                // sc.m_running += 1f - Mathf.Max(0, (neighbour.transform.position - sc.transform.position).magnitude / l_i); // decrease influence of running sheep with their distance
                break;
            }

            // mean distance to topologic neighbours
            sc.l_i += (sc.transform.position - neighbour.transform.position).sqrMagnitude;
          }

          // divide with number of topologic neighbours
          if (sc.topologicNeighbours.Count > 0)
            sc.l_i /= sc.topologicNeighbours.Count;
          sc.l_i = Mathf.Sqrt(sc.l_i);
        }
      }
    }
  }
}
