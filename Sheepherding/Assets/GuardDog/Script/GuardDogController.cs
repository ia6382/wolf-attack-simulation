using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GuardDogController : MonoBehaviour
{
  public int id;

  // state
  [HideInInspector]
  public Enums.DogState dogState;
  public Enums.DogState previousDogState;


  // GameManager
  private GameManager GM;

  // Dogs animator controller
  public Animator anim;

  // Movement parameters
  private float desiredV = .0f;
  private float v;

  private float theta;
  private float desiredTheta = .0f;
  private float eps = 0.2f;
  private Vector3 desiredThetaVector = new Vector3();
  private Vector3 separationVector = new Vector3();
	private float collisionSphereRadius;
	private float collisionAvoidWeight;

  // animation bools
  private bool turningLeft = false;
  private bool turningRight = false;
  private ParticleSystem partSys;

  // Start is called before the first frame update
  void Start()
  {
    // GameManager
    GM = FindObjectOfType<GameManager>();

    // init speed
    desiredV = 5.0f;
    v = desiredV;

    // heading is current heading
    desiredTheta = Mathf.Atan2(transform.forward.z, transform.forward.y) * Mathf.Rad2Deg;
    theta = desiredTheta;
    transform.forward = new Vector3(Mathf.Cos(theta * Mathf.Deg2Rad), .0f, Mathf.Sin(theta * Mathf.Deg2Rad)).normalized;

		//collision private parameters
		collisionSphereRadius = GM.DogsCollisionAvoidance.collisionSphereRadius;
		collisionAvoidWeight = GM.DogsCollisionAvoidance.collisionAvoidWeight;
  }

  // Update is called once per frame
  void Update()
  {
    LivestockGuardDog();
  }

  void FixedUpdate()
  {
    DogMovement();
  }
  
  // Livestock Guard Dog behaviour
  void LivestockGuardDog()
  {
    /* GUARD DOG LOGIC */
    /* behaviour logic */

    /*
     * There are two main behaviour logics to go on here:
     *    - move between the closest wolf and the closest sheep
     *    - chase the wolves away from sheep
     *    
     * There is one more thing we have to take into account here:
     *  livestock guard dogs (LGD) has a parameter, that tells him,
     *  how close the wolf has to be, to be considered a threat and
     *  also a parameter, how close the wolf has to be to be attacked.
     *  Reminder, distance for threat is always further than distance to attack
     *  
     * Besides that, the LGD has similar effect on closing to the sheep like
     * wolf. Also, let's say, there is another problem with this method. If no
     * sheep is endangered, LGD should try and keep the sheeps togheter, since they
     * behave as a sheepherd dog.
     * 
     */

    List<SheepController> sheep = new List<SheepController>(GM.sheepList)
      .Where(sc => !sc.dead)
      .ToList();

    if (GM.guardDogTactic.occlusion)
      sheep.Where(sc => IsVisible(sc, GM.guardDogTactic.blindAngle)).ToList();
    sheep.Sort(new ByDistanceFrom(transform.position));

    if(GM.guardDogTactic.local)
      sheep = sheep.GetRange(0, Mathf.Min(sheep.Count, GM.guardDogTactic.ns));

    // get all wolves in the scene that are not eating
    //List<WolfController> wolfs = new List<WolfController>(GM.wolfList).Where(w => !w.IsEating).ToList();
		List<WolfController> wolfs = GM.wolfList;
    wolfs.Sort(new ByDistanceFrom(transform.position));
    
    if (sheep.Count > 0)
    {
      if (GM.guardDogTactic.attackTactic)
      {
        float toWolfDist = Mathf.Infinity;

        if (wolfs.Count > 0)
          toWolfDist = (transform.position - wolfs[0].transform.position).magnitude;

        float dist;
        float sd = Mathf.Infinity;
        WolfController cw = null;
        SheepController cs = null;
        eps = 0.0f;

        // calculate the closest wolf-sheep pair
        foreach (WolfController wolf in wolfs)
        {
          sheep.Sort(new ByDistanceFrom(wolf.transform.position));

          dist = (wolf.transform.position - sheep[0].transform.position).magnitude;

          if (dist < sd)
          {
            sd = dist;
            cw = wolf;
            cs = sheep[0];
          }
        }

        if (cw)
        {
          // check the distance, according to ra and rd
          if (sd < GM.guardDogTactic.rd)
          {
            // attack the wolf, aka get him away from the herd
            Vector3 Pd = cw.transform.position - (cw.transform.position - cs.transform.position).normalized * GM.guardDogTactic.safeDistance;

            Debug.DrawCircle(Pd, .9f, new Color(0f, 0f, 1f, 1f));

            desiredThetaVector = (Pd - transform.position).normalized;

            dogState = Enums.DogState.running;
						desiredV = UnityEngine.Random.Range(GM.dogRunningSpeed-0.5f, GM.dogRunningSpeed+0.5f);
          }
          else if (sd < GM.guardDogTactic.ra)
          {
            // intercept the wolf before he comes too close to the herd
            Vector3 Pd = cw.transform.position - (cw.transform.position - cs.transform.position).normalized * GM.guardDogTactic.intersectDistance;

            Debug.DrawCircle(Pd, .9f, new Color(0f, 0f, 1f, 1f));

            desiredThetaVector = (Pd - transform.position).normalized;

            dogState = Enums.DogState.running;
						desiredV = UnityEngine.Random.Range(GM.dogRunningSpeed-0.5f, GM.dogRunningSpeed+0.5f);
          }
          else
          {
            // sheeps are safe, make sure, they stay in the group
            HerdTogheter(sheep);
            dogState = Enums.DogState.running;
						desiredV = UnityEngine.Random.Range(GM.dogRunningSpeed-0.5f, GM.dogRunningSpeed+0.5f);
          }
        }
        else
        {
          HerdTogheter(sheep);
          dogState = Enums.DogState.running;
					desiredV = UnityEngine.Random.Range(GM.dogRunningSpeed-0.5f, GM.dogRunningSpeed+0.5f);
        }
      }
      else
      {
        // chase the closest wolf away from the CM of sheep
        // calculate the center of mass
        Vector3 CM = new Vector3();
        foreach (SheepController sc in sheep)
          CM += sc.transform.position;
        CM /= (float)sheep.Count;

        if(wolfs.Count > 0)
        {
          WolfController wolf = wolfs[0];

          // chase the wolf away from the CM
          Vector3 Pd = wolf.transform.position - (wolf.transform.position - CM).normalized * GM.guardDogTactic.safeDistance;

          desiredThetaVector = (Pd - transform.position).normalized;

          Debug.DrawCircle(Pd, 1.0f, new Color(0, 0, 1));

          dogState = Enums.DogState.running;
					desiredV = UnityEngine.Random.Range(GM.dogRunningSpeed-0.5f, GM.dogRunningSpeed+0.5f);
        } else
        {
          HerdTogheter(sheep);

          dogState = Enums.DogState.running;
					desiredV = UnityEngine.Random.Range(GM.dogRunningSpeed-0.5f, GM.dogRunningSpeed+0.5f);
        }
      }
    }
    /* end of behaviour logic */

  }

  private void HerdTogheter(List<SheepController> sheep)
  {
    // calculate the center of mass
    Vector3 CM = new Vector3();
    foreach (SheepController sc in sheep)
      CM += sc.transform.position;
    CM /= (float)sheep.Count;

    // get the sheep furthest away from CM
    sheep.Sort(new ByDistanceFrom(CM));
    SheepController sheep_c = sheep[sheep.Count - 1];
    float gd = (sheep_c.transform.position - CM).magnitude;

    // get estimated size of the herd
    float ro = GM.SheepParametersStrombom.r_a;
    // aproximate radius of a circle
    float f_N = ro * Mathf.Pow(sheep.Count, 2f / 3f);
    // draw aprox herd size
    Debug.DrawCircle(CM, f_N, new Color(1f, 0f, 0f, 1f));

    if (gd > f_N)
    {
      // get the furthest sheep back into the herd
      Vector3 Pc = sheep_c.transform.position + (sheep_c.transform.position - CM).normalized * ro;
      Debug.DrawCircle(Pc, 0.6f, new Color(0f, 0f, 1f, 1f));
      desiredThetaVector = Pc - transform.position;
    }
  }

  bool IsHeadingForCollision()
  {
    RaycastHit hit;
		if (Physics.SphereCast(transform.position, GM.DogsCollisionAvoidance.collisionSphereRadius, transform.forward, out hit, GM.DogsCollisionAvoidance.collisionAvoidDst, GM.DogsCollisionAvoidance.obstacleMask))
    {
			#if DEBUG
			Debug.DrawRay(transform.position, transform.forward * GM.DogsCollisionAvoidance.collisionAvoidDst, Color.cyan);
			#endif
			collisionSphereRadius = GM.DogsCollisionAvoidance.collisionSphereRadius;
			collisionAvoidWeight = GM.DogsCollisionAvoidance.collisionAvoidWeight;
      return true;
    }
		else if(Physics.SphereCast(transform.position, 0.1f, transform.forward, out hit, GM.DogsCollisionAvoidance.collisionAvoidDst, GM.DogsCollisionAvoidance.obstacleMask)) //check if we are so close the sphere doesnt register obstacle
		{
			#if DEBUG	
			Debug.DrawRay(transform.position, transform.forward * GM.DogsCollisionAvoidance.collisionAvoidDst, Color.blue);
			#endif
			collisionSphereRadius = 0.1f; //in that case make the sphere smallere
			collisionAvoidWeight = GM.DogsCollisionAvoidance.collisionAvoidWeight*10f;
			return true;
		}
    else
    {
      return false;
    }
  }

  Vector3 FindDir()
  {
    Vector3[] rayDirections = CreateRayDirectionsDisk.directions;

    for (int i = 0; i < rayDirections.Length; i++)
    {
      Vector3 dir = transform.TransformDirection(rayDirections[i]);
      Ray ray = new Ray(transform.position, dir);
      if (!Physics.SphereCast(ray, collisionSphereRadius, GM.DogsCollisionAvoidance.collisionAvoidDst, GM.DogsCollisionAvoidance.obstacleMask))
      {
#if DEBUG
        Debug.DrawRay(transform.position, ray.direction * GM.DogsCollisionAvoidance.collisionAvoidDst, Color.green, 0f);
#endif
        return dir.normalized;
      }
#if DEBUG
      else
      {
        Debug.DrawRay(transform.position, ray.direction * GM.DogsCollisionAvoidance.collisionAvoidDst, Color.red, 0f);
      }
#endif
    }

    return transform.forward; //if we did not find a suitable dir, proceed forward
  }

  private void DogMovement()
  {
    // stop the dog if he is too close to the wolf that is not eating
    foreach(WolfController wolf in GM.wolfList)
    {
      //if (!wolf.IsEating && (wolf.transform.position - transform.position).magnitude < GM.guardDogTactic.safeDistance -1.5f)
			if ((wolf.transform.position - transform.position).magnitude < GM.guardDogTactic.safeDistance -1.5f)
      {
        desiredV = 0f;
        dogState = Enums.DogState.idle;
      } 
			//else if (!wolf.IsEating && (wolf.transform.position - transform.position).magnitude < GM.guardDogTactic.safeDistance)// + 1.5f)
			else if ((wolf.transform.position - transform.position).magnitude < GM.guardDogTactic.safeDistance)// + 1.5f)
      {
        // if close to too close, steer it away from the wolf in an arc *FIXED: if close bark around dog, if even closer stand ground
        Vector3 outP = (transform.position - wolf.transform.position).normalized;
        desiredThetaVector += outP;
      }
    }

    // steer dog away from dogs nearby
    separationVector = new Vector3();
    foreach(GuardDogController dog in GM.guardDogList)
    {
      if ((dog.transform.position - transform.position).magnitude < GM.guardDogTactic.separationDistance)
      {
        separationVector -= (dog.transform.position - transform.position).normalized;
      }
    }

    separationVector = separationVector.normalized;

		//collision avoidance
		Vector3 collisionAvoidDir = new Vector3();
		if (IsHeadingForCollision())
		{
			collisionAvoidDir = FindDir();
		}

		// add to final direction vector
		desiredThetaVector = desiredThetaVector.normalized + collisionAvoidDir * collisionAvoidWeight + separationVector;

    // extract desired heading
    desiredTheta = (Mathf.Atan2(desiredThetaVector.z, desiredThetaVector.x) + eps) * Mathf.Rad2Deg;

    // compute angular change based on max angular velocity and desiredTheta
    theta = Mathf.MoveTowardsAngle(theta, desiredTheta, GM.wolfMaxTurn * Time.deltaTime);
    // ensure angle remains in [-180,180)
    theta = (theta + 180f) % 360f - 180f;
    // compute longitudinal velocity change based on max longitudinal acceleration and desiredV
    v = Mathf.MoveTowards(v, desiredV, GM.wolfMaxSpeedChange * Time.deltaTime);
    // ensure speed remains in [minSpeed, maxSpeed]
    v = Mathf.Clamp(v, GM.wolfMinSpeed, GM.wolfMaxSpeed);

    // compute new forward direction
    Vector3 newForward = new Vector3(Mathf.Cos(theta * Mathf.Deg2Rad), .0f, Mathf.Sin(theta * Mathf.Deg2Rad)).normalized;
    // update position
    Vector3 newPosition = transform.position + (Time.deltaTime * v * newForward);
    // force ground, to revert coliders making sheep fly
    newPosition.y = 0f;

    transform.position = newPosition;
    transform.forward = newForward;

    // draw dogRepulsion radius
    if (GM.StrombomSheep)
    {
      Debug.DrawCircle(transform.position, GM.SheepParametersStrombom.r_s, new Color(0f, 1f, 1f, .5f), true);
      Debug.DrawCircle(transform.position, GM.SheepParametersStrombom.r_sS, new Color(0f, 1f, 1f, 1f));
    }
    else
    {
      Debug.DrawCircle(transform.position, GM.SheepParametersGinelli.r_s, new Color(0f, 1f, 1f, .5f), true);
      Debug.DrawCircle(transform.position, GM.SheepParametersGinelli.r_sS, new Color(0f, 1f, 1f, 1f));
    }

    if (v == .0f)
    {
      turningLeft = false;
      turningRight = false;
    }
    else
    {
      turningLeft = theta > 0;
      turningRight = theta < 0;
    }

    // Animation Controller
    anim.SetBool("IsRunning", v > 6);
    anim.SetBool("Reverse", v < 0);
    anim.SetBool("IsWalking", (v > 0 && v <= 6) ||
                               turningLeft ||
                               turningRight);
  }

  private bool IsVisible(SheepController sc, float blindAngle)
  {
#if true // experimental: test occlusion
    Vector3 Cm = GetComponent<Rigidbody>().worldCenterOfMass;
    Vector3 toCm = sc.GetComponent<Rigidbody>().worldCenterOfMass - Cm;
    bool hit = Physics.Raycast(Cm + .5f * toCm.normalized, toCm.normalized, toCm.magnitude - 1f);
    if (hit) return false;
#endif
    Vector3 toSc = sc.transform.position - transform.position;
    float cos = Vector3.Dot(transform.forward, toSc.normalized);
    return cos > Mathf.Cos((180f - blindAngle / 2f) * Mathf.Deg2Rad);
  }

}
