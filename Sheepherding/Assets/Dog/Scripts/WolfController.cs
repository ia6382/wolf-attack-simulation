using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System;

public class WolfController : MonoBehaviour
{
  public int id;

  // state
  [HideInInspector]
  public Enums.DogState dogState;
  public Enums.DogState previousDogState;

  // Game settings
  [HideInInspector]
  public int controls;

  // GameManager
  private GameManager GM;

  // Dogs animator controller
  public Animator anim;

  // Movement parameters
  private float desiredV = .0f;
  private float v;
	private bool eating;
	private bool focused;
	private SheepController killedSheep;
	private SheepController huntedSheep;

  public bool IsEating
  {
    get
    {
      return this.eating;
    }
  }

	private float collisionSphereRadius;
	private float collisionAvoidWeight;

  private float theta;
  private float desiredTheta = .0f;
  private float eps = 0.2f;
  private Vector3 desiredThetaVector = new Vector3();
  

  // animation bools
  private bool turningLeft = false;
  private bool turningRight = false;
	private ParticleSystem partSys;

  // Use this for initialization
  void Start()
  {
    // GameManager
    GM = FindObjectOfType<GameManager>();

    // init speed
		eating = false;
		focused = false;
    desiredV = 5.0f;
    v = desiredV;

    // heading is current heading
    desiredTheta = Mathf.Atan2(transform.forward.z, transform.forward.y) * Mathf.Rad2Deg;
    theta = desiredTheta;
    transform.forward = new Vector3(Mathf.Cos(theta * Mathf.Deg2Rad), .0f, Mathf.Sin(theta * Mathf.Deg2Rad)).normalized;

		//particle system
		partSys = transform.Find("Particle System").GetComponent<ParticleSystem>();

		//collision private parameters
		collisionSphereRadius = GM.DogsCollisionAvoidance.collisionSphereRadius;
		collisionAvoidWeight = GM.DogsCollisionAvoidance.collisionAvoidWeight;
  }

  // Update is called once per frame
  void Update()
  {
    if (GM.BehaviourWolves)
    {
      switch (GM.WolfAttack.at)
      {
        case Enums.AttackTactic.center:
        case Enums.AttackTactic.closest:
        case Enums.AttackTactic.isolated:
          SingleWolfBehaviour();
          break;
        case Enums.AttackTactic.strombom:
          WolfPackStrombom();
          break;
        case Enums.AttackTactic.boids:
					BoidWolfPack();
					break;
      }
    }
    else
      Controls();
  }

  void FixedUpdate() {
    WolfMovement();
  }

  void Controls()
  {
    if(Input.GetKey(KeyCode.W))
    {
      desiredV += GM.wolfMaxSpeedChange * Time.deltaTime;
    }
    else if (Input.GetKey(KeyCode.S))
    {
      desiredV -= GM.wolfMaxSpeedChange * Time.deltaTime;
    }
    else
    {
      desiredV = Mathf.MoveTowards(desiredV, 0, GM.wolfMaxSpeedChange / 10f); 
    }

    if (Input.GetKey(KeyCode.A))
    {
      desiredTheta += GM.wolfMaxTurn * Time.deltaTime;
    }
    else if (Input.GetKey(KeyCode.D))
    {
      desiredTheta -= GM.wolfMaxTurn * Time.deltaTime;
    }
    else
    {
      desiredTheta = theta + Mathf.MoveTowardsAngle((desiredTheta - theta), 0, GM.wolfMaxTurn / 10f);
      // ensure angle remains in [-180,180)
      desiredTheta = (desiredTheta + 180f) % 360f - 180f;
    }
  }

  private bool IsVisible(SheepController sc, float blindAngle)
  {
#if true // experimental: test occlusion
    Vector3 Cm = GetComponent<Rigidbody>().worldCenterOfMass;
    Vector3 toCm = sc.GetComponent<Rigidbody>().worldCenterOfMass - Cm;
    bool hit = Physics.Raycast(Cm + .5f*toCm.normalized, toCm.normalized, toCm.magnitude - 1f);
    if (hit) return false;
#endif
    Vector3 toSc = sc.transform.position - transform.position;
    float cos = Vector3.Dot(transform.forward, toSc.normalized);
    return cos > Mathf.Cos((180f - blindAngle / 2f) * Mathf.Deg2Rad);
  }
  
  void SingleWolfBehaviour()
  {
    /* IMPLEMENT WOLF LOGIC HERE */
    /* behavour logic */

    List<SheepController> sheep = new List<SheepController>(GM.sheepList)
      .Where(sc => !sc.dead)
      .Where(sc => IsVisible(sc, GM.WolfsParametersStrombom.blindAngle))
      .ToList();

    // only take into the account the nearest 7 sheeps or whatever is written in ns parameter
    sheep.Sort(new ByDistanceFrom(transform.position));
    sheep = sheep.GetRange(0, Mathf.Min(sheep.Count, GM.WolfsParametersStrombom.ns));

    if (sheep.Count > 0)
    {
      if (focused)
      {
        desiredThetaVector = huntedSheep.transform.position - transform.position;
        desiredV = GM.wolfRunningSpeed;

        //debug mark target
        Debug.DrawCircle(huntedSheep.transform.position, 2.0f, new Color(1f, 0f, 1f, 1f));
      } else
      {
        if (GM.WolfAttack.at == Enums.AttackTactic.closest)
        {
          // attack closest sheep
          Vector3 sCM = sheep[0].transform.position;
          desiredThetaVector = (sCM - transform.position).normalized;

          StartCoroutine("Focus", sheep[0]);
          Debug.DrawCircle(sheep[0].transform.position, 2.0f, new Color(0f, 0f, 1f, 1f));
        }
        else if(GM.WolfAttack.at == Enums.AttackTactic.isolated)
        {
          // attack the most isolated sheep

          ByAngleFrom angleCalculator = new ByAngleFrom(transform.position);

          // sort them in the clockwise manner
          sheep.Sort(angleCalculator);

          float[] angles = new float[sheep.Count];

          // calculate the inbetween angles from neighbours, exceptions for first and last sheep
          for (int i = 0; i < sheep.Count; i++)
          {
            float leftAngle = Mathf.Infinity;
            float rightAngle = Mathf.Infinity;

            if (i != 0)
            {
              leftAngle = angleCalculator.Calculate(sheep[i - 1], sheep[i]);
            } else
            {
              leftAngle = angleCalculator.Calculate(sheep[0], sheep[sheep.Count - 1]);
            }

            if (i != sheep.Count - 1)
            {
              rightAngle = angleCalculator.Calculate(sheep[i], sheep[i + 1]);
            } else
            {
              rightAngle = angleCalculator.Calculate(sheep[i], sheep[0]);
            }

            angles[i] = Mathf.Min(leftAngle, rightAngle);
          }

          float minValue = angles.Max();
          int i_sheep = angles.ToList().IndexOf(minValue);

          desiredThetaVector = (sheep[i_sheep].transform.position - transform.position).normalized;

          StartCoroutine("Focus", sheep[i_sheep]);

          Debug.DrawCircle(sheep[i_sheep].transform.position, 2.0f, new Color(0f, 1f, 0f, 1f));

        }
        else if (GM.WolfAttack.at == Enums.AttackTactic.center)
        {
          // attacking a center of visible sheeps

          // calculate the CM of sheep
          Vector3 CM = new Vector3();
          foreach (SheepController sc in sheep)
          {
            CM += sc.transform.position;
            Debug.DrawCircle(sc.transform.position, 1.0f, new Color(1f, 0f, 0f, 1f));
          }
          if (sheep.Count > 0)
            CM /= (float)sheep.Count;

          // set the theta vector into direction of CM
          desiredThetaVector = (CM - transform.position).normalized;

          Debug.DrawCircle(CM, 2.0f, new Color(0f, 1f, 0f, 1f));

        }
      }
      desiredV = GM.wolfRunningSpeed;
    }
    else
    {
      dogState = Enums.DogState.walking;
      desiredV = GM.wolfWalkingSpeed;
    }
    
    /* end of behaviour logic */
  }


  void WolfPackStrombom()
  {
    /* IMPLEMENT DOG LOGIC HERE */
    /* behavour logic */

    List<WolfController> wolfs = new List<WolfController>(GM.wolfList).Where(d => d != GetComponent<WolfController>()).ToList();
    List<WolfController> allWolfs = new List<WolfController>(GM.wolfList);

    Vector3 wolfCM = new Vector3();
    Vector3 fwolfCM = new Vector3();
    foreach (WolfController d in wolfs)
      wolfCM += d.transform.position;
    fwolfCM = wolfCM + transform.position;

    if (wolfs.Count > 0) { }
      wolfCM /= wolfs.Count;
    fwolfCM /= wolfs.Count + 1;

    Debug.DrawCircle(wolfCM, .5f, new Color(0f, 0f, 1f, 1f));

    List<SheepController> sheep;
    if (!focused)
    {
      // get only live sheep
      sheep = new List<SheepController>(GM.sheepList).Where(sc => !sc.dead).ToList();
      if (GM.WolfsParametersStrombom.local)
      { // localized perception
        if (GM.WolfsParametersStrombom.occlusion)
          sheep = sheep.Where(sc => IsVisible(sc, GM.WolfsParametersStrombom.blindAngle)).ToList();
        // take sheep closest to dog CM - should be same for all dogs (wolves)
        sheep.Sort(new ByDistanceFrom(fwolfCM));
        sheep = sheep.GetRange(0, Mathf.Min(1, sheep.Count));
      }
    }
    else
    {
      sheep = new List<SheepController>() { huntedSheep };
    }
    

    if (sheep.Count > 0)
    {
      // compute CM of sheep
      Vector3 CM = new Vector3();
      foreach (SheepController sc in sheep)
        CM += sc.transform.position;
      CM /= (float)sheep.Count;

      // draw CM
      Vector3 X = new Vector3(1, 0, 0);
      Vector3 Z = new Vector3(0, 0, 1);
      Color color = new Color(0f, 0f, 0f, 1f);
      Debug.DrawRay(CM - X, 2 * X, color);
      Debug.DrawRay(CM - Z, 2 * Z, color);


      // find distance of sheep that is nearest to the dog & distance of sheep furthest from CM
      float md_ds = Mathf.Infinity;
      SheepController sheep_c = null; // sheep furthest from CM
      float Md_sC = 0;

      foreach (SheepController sc in sheep)
      {
        // distance from CM
        float d_sC = (CM - sc.transform.position).magnitude;
        if (d_sC > Md_sC)
        {
          Md_sC = d_sC;
          sheep_c = sc;
        }

        // distance from dog
        float d_ds = (sc.transform.position - transform.position).magnitude;
        md_ds = Mathf.Min(md_ds, d_ds);
      }

      float ro = 0; // mean nnd
      if (GM.StrombomSheep)
        ro = GM.SheepParametersStrombom.r_a;
      else
        ro = GM.SheepParametersGinelli.r_0;

      float r_s = GM.WolfsParametersStrombom.r_s * ro; // compute true stopping distance
      float r_w = GM.WolfsParametersStrombom.r_w * ro; // compute true walking distance
      float r_r = GM.WolfsParametersStrombom.r_r * ro; // compute true running distance

      dogState = Enums.DogState.running;
      desiredV = GM.wolfRunningSpeed;

      //if (md_ds < r_s)
      //{
      //  dogState = Enums.DogState.idle;
      //  desiredV = .0f;
      //}

      // if close to any sheep start walking
      //if (md_ds < r_w)
      //{
      //  dogState = Enums.DogState.walking;
      //  desiredV = GM.wolfWalkingSpeed;
      //}
      //else if (md_ds > r_r)
      //{
      //  // default run in current direction
      //  dogState = Enums.DogState.running;
      //  desiredV = GM.wolfRunningSpeed;
      //}

      // aproximate radius of a circle
      float f_N = ro * Mathf.Pow(sheep.Count, 2f / 3f);
      // draw aprox herd size
      Debug.DrawCircle(CM, f_N, new Color(1f, 0f, 0f, 1f));

      foreach (SheepController sc in sheep)
        Debug.DrawCircle(sc.transform.position, .5f, new Color(1f, 0f, 0f, 1f));

      // if all agents in a single compact group, collect them
      if (Md_sC < f_N)
      {
        // instead of barn herd it into the CM of dogs

        // compute position so that the GCM is on a line between the wolf and the target
        Vector3 Pd = CM + (CM - wolfCM).normalized * ro * Mathf.Sqrt(sheep.Count); // Mathf.Min(ro * Mathf.Sqrt(sheep.Count), Md_sC);

        Debug.DrawCircle(Pd, .9f, new Color(0f, 0f, 1f, 1f));

        // if wolf is too close to the sheep, steer him away in an arc around the sheep
        if (md_ds < (GM.SheepParametersStrombom.r_s + GM.SheepParametersStrombom.r_sS) / 2.0f)
        {
          Vector3 outP = (transform.position - sheep[0].transform.position).normalized;
          Vector3 inP = (Pd - transform.position).normalized;

          desiredThetaVector = outP + inP;
          Debug.DrawRay(transform.position, desiredThetaVector, new Color(0f, 1f, 1f, 1f));
        }
        else
        {
          desiredThetaVector = Pd - transform.position;
        }

        if (desiredThetaVector.magnitude > r_w)
          desiredV = GM.wolfRunningSpeed;

        color = new Color(0f, 1f, 0f, 1f);
        Debug.DrawRay(Pd - X - Z, 2 * X, color);
        Debug.DrawRay(Pd + X - Z, 2 * Z, color);
        Debug.DrawRay(Pd + X + Z, -2 * X, color);
        Debug.DrawRay(Pd - X + Z, -2 * Z, color);
      }
      else
      {
        // compute position so that the sheep most distant from the GCM is on a line between the dog and the GCM
        Vector3 Pc = sheep_c.transform.position + (sheep_c.transform.position - CM).normalized * ro;
        // move in an arc around the herd??
        desiredThetaVector = Pc - transform.position;

        color = new Color(1f, .5f, 0f, 1f);
        Debug.DrawRay(Pc - X - Z, 2 * X, color);
        Debug.DrawRay(Pc + X - Z, 2 * Z, color);
        Debug.DrawRay(Pc + X + Z, -2 * X, color);
        Debug.DrawRay(Pc - X + Z, -2 * Z, color);
      }
    }
    else
    {
      dogState = Enums.DogState.idle;
      desiredV = .0f;
    }
  }


  void BoidWolfPack()
  {
    /* IMPLEMENT DOG LOGIC HERE */
    /* behaviour logic */
		List<WolfController> wolfs = new List<WolfController>(GM.wolfList)
      .Where(d => d != GetComponent<WolfController>())
      .Where(w => !w.IsEating)
      .ToList();
    List<SheepController> sheep = new List<SheepController>(GM.sheepList)
      .Where(sc => !sc.dead)
			.Where(sc => IsVisible(sc, GM.WolfsParametersStrombom.blindAngle))
      .ToList();

    // only take into the account the nearest 7 sheeps or whatever is written in ns parameter
    sheep.Sort(new ByDistanceFrom(transform.position));
		sheep = sheep.GetRange(0, Mathf.Min(sheep.Count, GM.WolfsParametersStrombom.ns));

    Vector3 cohesion = new Vector3();
    Vector3 allignment = new Vector3();
    Vector3 separation = new Vector3();

		foreach(WolfController dc in wolfs)
    {
      // calculate the distance to each wolf
      Vector3 dir = (dc.transform.position - transform.position);
      float dist = dir.magnitude;

      if (GM.WolfsBoidParameters.ra < dist && dist <= GM.WolfsBoidParameters.rc)
      {
        cohesion += dir.normalized;
      } else if(GM.WolfsBoidParameters.rs < dist && dist <= GM.WolfsBoidParameters.ra)
      {
        allignment += dc.desiredThetaVector.normalized;
      } else if(dist <= GM.WolfsBoidParameters.rs)
      {
        separation += -dir.normalized;
      }
    }

    cohesion = cohesion.normalized * GM.WolfsBoidParameters.cohesionFactor;
    allignment = allignment.normalized * GM.WolfsBoidParameters.allignmentFactor;
    separation = separation.normalized * GM.WolfsBoidParameters.separationFactor;
    
    Vector3 gt = new Vector3();
    foreach(SheepController sc in sheep)
    {
      gt += (sc.transform.position - transform.position).normalized;
    }

    gt = gt.normalized * GM.WolfsBoidParameters.gtFactor;

    // combine all forces to get the 
    desiredThetaVector = (cohesion + allignment + separation + gt).normalized;
    desiredV = GM.dogMaxSpeed;

    /* end behaviour logic */
  }

  void BehaviourLogicStrombom()
  {
    /* IMPLEMENT DOG LOGIC HERE */
    /* behavour logic */

    // get only live sheep
    List<SheepController> sheep = new List<SheepController>(GM.sheepList).Where(sc => !sc.dead).ToList();
    if (GM.WolfsParametersStrombom.local)
    { // localized perception
      if (GM.WolfsParametersStrombom.occlusion)
        sheep = sheep.Where(sc => IsVisible(sc, GM.WolfsParametersStrombom.blindAngle)).ToList();

#if false // experimental: exlude visually occludded sheep
      sheepList.Sort(new ByDistanceFrom(transform.position));
      List<int> hidden = new List<int>();
      for (int i = 0; i < sheepList.Count; i++)
      {
        Vector3 toSc = sheepList[i].transform.position - transform.position;
        float dcos = Mathf.Atan2(.5f*sheepList[i].transform.localScale.x, toSc.magnitude);
        float cos = Mathf.Acos(Vector3.Dot(transform.forward, toSc.normalized));
        for (int j = i+1; j < sheepList.Count; j++)
        {
          if (hidden.Exists(k => k == sheepList[j].id)) continue; // skip those already hidden

          Vector3 toSc2 = sheepList[j].transform.position - transform.position;
          float dcos2 = Mathf.Atan2(.5f*sheepList[j].transform.localScale.x, toSc2.magnitude);
          float cos2 = Mathf.Acos(Vector3.Dot(transform.forward, toSc2.normalized));

          float visible = Mathf.Max(0, Mathf.Min(cos - dcos, cos2 + dcos2) - (cos2 - dcos2));
          visible += Mathf.Max(0, (cos2 + dcos2) - Mathf.Max(cos2 - dcos2, cos + dcos));
          if (visible/dcos2 <= 1) hidden.Add(sheepList[j].id);
        }
      }
      for (int i = 0; i < sheepList.Count; i++)
      {
        if (!hidden.Exists(j => j == sheepList[i].id))
          Debug.DrawRay(transform.position, sheepList[i].transform.position - transform.position, Color.white);
      }

      sheepList = sheepList.Where(sheep => !hidden.Exists(id => id == sheep.id)).ToList();
#endif
#if true // take into account cognitive limits track max ns nearest neighbours
      sheep.Sort(new ByDistanceFrom(transform.position));
      sheep = sheep.GetRange(0, Mathf.Min(GM.WolfsParametersStrombom.ns, sheep.Count));
#endif
    }

    if (sheep.Count > 0)
    {
      // compute CM of sheep
      Vector3 CM = new Vector3();
      foreach (SheepController sc in sheep)
        CM += sc.transform.position;
      if (sheep.Count > 0)
        CM /= (float)sheep.Count;

      // draw CM
      Vector3 X = new Vector3(1, 0, 0);
      Vector3 Z = new Vector3(0, 0, 1);
      Color color = new Color(0f, 0f, 0f, 1f);
      Debug.DrawRay(CM - X, 2 * X, color);
      Debug.DrawRay(CM - Z, 2 * Z, color);

      // find distance of sheep that is nearest to the dog & distance of sheep furthest from CM
      float md_ds = Mathf.Infinity;
      SheepController sheep_c = null; // sheep furthest from CM
      float Md_sC = 0;

      foreach (SheepController sc in sheep)
      {
        // distance from CM
        float d_sC = (CM - sc.transform.position).magnitude;
        if (d_sC > Md_sC)
        {
          Md_sC = d_sC;
          sheep_c = sc;
        }

        // distance from dog
        float d_ds = (sc.transform.position - transform.position).magnitude;
        md_ds = Mathf.Min(md_ds, d_ds);
      }

      float ro = 0; // mean nnd
      if (GM.StrombomSheep)
        ro = GM.SheepParametersStrombom.r_a;
      else
        ro = GM.SheepParametersGinelli.r_0;

#if false // aproximate interaction distance through nearest neigbour distance
      foreach (SheepController sheep in sheepList)
      {
        float nn = Mathf.Infinity;
        foreach (SheepController sc in sheepList)
        {
          if (sc.id == sheep.id) continue;
          nn = Mathf.Min(nn, (sheep.transform.position - sc.transform.position).magnitude);
        }
        ro += nn;
      }
      ro /= sheepList.Count;
#endif

      float r_s = GM.WolfsParametersStrombom.r_s * ro; // compute true stopping distance
      float r_w = GM.WolfsParametersStrombom.r_w * ro; // compute true walking distance
      float r_r = GM.WolfsParametersStrombom.r_r * ro; // compute true running distance

      // if too close to any sheep stop and wait
#if false
      if (md_ds < r_s)
      {
          dogState = Enums.DogState.idle;
          desiredV = .0f;
      }
      // if close to any sheep start walking
      if (md_ds < r_w)
      {
        dogState = Enums.DogState.walking;
        desiredV = GM.dogWalkingSpeed;
      }
      else if (md_ds > r_r)
      {
        // default run in current direction
        dogState = Enums.DogState.running;
        desiredV = GM.dogRunningSpeed;
      }
#else
      dogState = Enums.DogState.running;
      desiredV = GM.wolfRunningSpeed;
#endif

      // aproximate radius of a circle
      float f_N = ro * Mathf.Pow(sheep.Count, 2f / 3f);
      // draw aprox herd size
      Debug.DrawCircle(CM, f_N, new Color(1f, 0f, 0f, 1f));

#if true
      foreach (SheepController sc in sheep)
        Debug.DrawCircle(sc.transform.position, .5f, new Color(1f, 0f, 0f, 1f));
#endif
			//still focused on hunting sheep, ignore new targets
			if(focused)
			{
				desiredThetaVector = huntedSheep.transform.position - transform.position;
				desiredV = GM.wolfRunningSpeed;

				//debug mark target
				Vector3 Pc = huntedSheep.transform.position;
				color = new Color(1f, 0f, 0f, 1f);
				Debug.DrawRay(Pc - X - Z, 2 * X, color);
				Debug.DrawRay(Pc + X - Z, 2 * Z, color);
				Debug.DrawRay(Pc + X + Z, -2 * X, color);
				Debug.DrawRay(Pc - X + Z, -2 * Z, color);
			}
			else
			{	// if all agents in a single compact group, collect them
	      if (Md_sC < f_N)
	      {
	        BarnController barn = FindObjectOfType<BarnController>();

	        // compute position so that the GCM is on a line between the dog and the target
	        Vector3 Pd = CM + (CM - barn.transform.position).normalized * ro * Mathf.Sqrt(sheep.Count); // Mathf.Min(ro * Mathf.Sqrt(sheep.Count), Md_sC);
	        desiredThetaVector = Pd - transform.position;

	        if (desiredThetaVector.magnitude > r_w)
	          desiredV = GM.wolfRunningSpeed;

	        color = new Color(0f, 1f, 0f, 1f);
	        Debug.DrawRay(Pd - X - Z, 2 * X, color);
	        Debug.DrawRay(Pd + X - Z, 2 * Z, color);
	        Debug.DrawRay(Pd + X + Z, -2 * X, color);
	        Debug.DrawRay(Pd - X + Z, -2 * Z, color);
	      }
	      else
	      {
	        // compute position so that the sheep most distant from the GCM is on a line between the dog and the GCM
	        Vector3 Pc = sheep_c.transform.position + (sheep_c.transform.position - CM).normalized * ro;
	        // move in an arc around the herd??
	        desiredThetaVector = Pc - transform.position;

	        color = new Color(1f, .5f, 0f, 1f);
	        Debug.DrawRay(Pc - X - Z, 2 * X, color);
	        Debug.DrawRay(Pc + X - Z, 2 * Z, color);
	        Debug.DrawRay(Pc + X + Z, -2 * X, color);
	        Debug.DrawRay(Pc - X + Z, -2 * Z, color);
	      }
				
				StartCoroutine ("Focus", sheep_c);
			}
    }
    else
    {
      dogState = Enums.DogState.idle;
      desiredV = .0f;
    }
    /* end of behaviour logic */
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
		//check with small sphere if we are so close that Unity big sphere cast doesnt register obstacle
		else if(Physics.SphereCast(transform.position, 0.1f, transform.forward, out hit, GM.DogsCollisionAvoidance.collisionAvoidDst, GM.DogsCollisionAvoidance.obstacleMask)) 
		{
			#if DEBUG	
			Debug.DrawRay(transform.position, transform.forward * GM.DogsCollisionAvoidance.collisionAvoidDst, Color.blue);
			#endif
			collisionSphereRadius = 0.1f;
			collisionAvoidWeight = GM.DogsCollisionAvoidance.collisionAvoidWeight*10f;
			return true;
		}
		else
		{
			return false;
		}
	}

	Vector3 FindDir () {
		Vector3[] rayDirections = CreateRayDirectionsDisk.directions;

		for (int i = 0; i < rayDirections.Length; i++) {
			Vector3 dir = transform.TransformDirection (rayDirections[i]);
			Ray ray = new Ray (transform.position, dir);
			if (!Physics.SphereCast (ray, collisionSphereRadius, GM.DogsCollisionAvoidance.collisionAvoidDst, GM.DogsCollisionAvoidance.obstacleMask)) {
#if DEBUG
				Debug.DrawRay(transform.position, ray.direction*GM.DogsCollisionAvoidance.collisionAvoidDst, Color.green, 0f);
#endif
				return dir.normalized;
			}
#if DEBUG
			else{
				Debug.DrawRay(transform.position, ray.direction*GM.DogsCollisionAvoidance.collisionAvoidDst, Color.red, 0f);
			}
#endif
		}

		return transform.forward; //if we did not find a suitable dir, proceed forward
	}

  void WolfMovement()
  {
    if (!eating)
    {
      // attacking sheep mechanism
      List<SheepController> sheep = new List<SheepController>(GM.sheepList)
        .Where(sc => !sc.dead)
        .ToList();
      foreach (SheepController sc in sheep)
      {
        float d_sC = (sc.transform.position - transform.position).magnitude;
        if (d_sC < GM.WolfAttack.attackDistance)
        {
          StartCoroutine("Wait", sc);
          break;
        }
      }
    }

    //collision avoidance
    Vector3 collisionAvoidDir = new Vector3();
    if (IsHeadingForCollision())
    {
      collisionAvoidDir = FindDir();
    }

    // eating
    if (eating)
    {
      desiredThetaVector = killedSheep.transform.position - transform.position;
      dogState = Enums.DogState.idle;
      desiredV = .0f;
    }
    // add to final direction vector
    desiredThetaVector = desiredThetaVector.normalized + collisionAvoidDir * collisionAvoidWeight;

    foreach(GuardDogController lgd in GM.guardDogList)
    {
      if ((lgd.transform.position - transform.position).magnitude < GM.WolfDefense.safeDistance)
      {
        desiredThetaVector = -(lgd.transform.position - transform.position).normalized;
      }
    }

    // check if wolf is too close to LGD
    foreach(GuardDogController gd in GM.guardDogList)
    {
      if( (gd.transform.position - transform.position).magnitude < GM.WolfDefense.safeDistance )
      {
        // if it is too close, steer the wolf away from the LGD
        desiredThetaVector = transform.position - gd.transform.position;
      }
    }

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
      Debug.DrawCircle(transform.position, GM.SheepParametersStrombom.r_s, new Color(1f, 1f, 0f, .5f), true);
      Debug.DrawCircle(transform.position, GM.SheepParametersStrombom.r_sS, new Color(1f, 1f, 0f, 1f));
    }
    else
    {
      Debug.DrawCircle(transform.position, GM.SheepParametersGinelli.r_s, new Color(1f, 1f, 0f, .5f), true);
      Debug.DrawCircle(transform.position, GM.SheepParametersGinelli.r_sS, new Color(1f, 1f, 0f, 1f));
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

	IEnumerator Wait(SheepController sc){
		killedSheep = sc;
		eating = true;
		sc.dead = true;
		GM.sheepCount--;
		partSys.Play();
		yield return new WaitForSeconds (GM.WolfAttack.eatingTime);
		sc.gameObject.SetActive(false);
		partSys.Stop();
		eating = false;
		focused = false;
	}

	IEnumerator Focus(SheepController sc){
		huntedSheep = sc;
		focused = true;
		yield return new WaitForSeconds (GM.WolfAttack.focusTime);
		focused = false;
	}

}
