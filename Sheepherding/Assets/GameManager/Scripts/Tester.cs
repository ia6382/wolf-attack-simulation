using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.IO;

public class Tester : MonoBehaviour
{
	[Header("Testing Parameters")]
	public int timePerSimulation = 10;
	public String fileName = "test1";
	public String filePath = ".\\Assets\\TestResults\\";
	[Header("README")]
	[TextArea]
	public string IMPORTANT = "Set number of simulations in SimulationNumber script in GM folder. \nDelete or change name of file before testing again.";

	private GameManager GM;
	private float gameTimer;
	private int numOfSheep;
	private StreamWriter sw;
	private String path;

    void Start()
    {
		GM = FindObjectOfType<GameManager>();
		gameTimer = timePerSimulation;
		numOfSheep = GM.nOfSheep;

		//get path of file
		path = filePath+fileName+".txt";

		//if file not exist write header
		if(!File.Exists(path))
		{
			//late start to tallow init in GM
			StartCoroutine("LateStart");
		}
    }


	IEnumerator LateStart()
	{
		yield return new WaitForSeconds(0.1f);
		//write header
		using (sw = File.AppendText(path)) 
		{
			sw.WriteLine("TEST PARAMETERS");
			sw.WriteLine("number of simulations: "+SimulationNumber.n);
			sw.WriteLine("Sheep:");
			sw.WriteLine("\t number of sheep: "+GM.nOfSheep);
			sw.WriteLine("\t occlusion: "+GM.SheepParametersStrombom.occlusion);
			sw.WriteLine("\t collision: "+GM.SheepCollisionAvoidance.SHEEPCOLLISION_ON);
			sw.WriteLine("Wolves:");
			sw.WriteLine("\t number of wolves: "+GM.wolfList.Count);
			sw.WriteLine("\t behaviour: "+GM.BehaviourWolves);
			sw.WriteLine("\t local: "+GM.WolfsParametersStrombom.local);
			sw.WriteLine("\t occlusion: "+GM.WolfsParametersStrombom.occlusion);
			sw.WriteLine("\t attack strategy: "+GM.WolfAttack.at);
			sw.WriteLine("Dogs:");
			sw.WriteLine("\t number of dogs: "+GM.guardDogList.Count);
			sw.WriteLine("\t tactic: "+GM.guardDogTactic.local);
			sw.WriteLine("\t occlusion: "+GM.guardDogTactic.occlusion);
			sw.WriteLine("\t attack tactic: "+GM.guardDogTactic.attackTactic);
			sw.WriteLine("\nTimes of sheep deaths:");
			sw.WriteLine("---");
		}	
	}
		
    void Update()
    {
		//time
		gameTimer -= Time.deltaTime;

		//write to file if sheep were killed this frame
		while(numOfSheep > GM.sheepCount){
			using (sw = File.AppendText(path)) 
			{
				sw.WriteLine(timePerSimulation - gameTimer);
			}
			numOfSheep --;
		}


		if (gameTimer <= 0 || GM.sheepCount <= 0)
		{
			SimulationNumber.n -= 1;
			if(SimulationNumber.n <= 0){
				Debug.Log("Quiting");
				UnityEditor.EditorApplication.isPlaying = false;
			}
			else{
				using (sw = File.AppendText(path)) 
				{
					sw.WriteLine("---");
				}
				Debug.Log("Restarting");
				SceneManager.LoadScene(SceneManager.GetActiveScene().name);
			}
		}

    }
}
