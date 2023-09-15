using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System;

//Navigation Nodes
public class LinkedNodes : MonoBehaviour, IComparable {

	public int index;
	public GameObject[] linkedNodeObjects;

	public int[] linkedNodesIndex;
	public LinkedNodes[] NodeSiblings { get; private set; }


	void Start () {
		//Get the correct index for each linked Node
		linkedNodesIndex = new int[linkedNodeObjects.Length];
		NodeSiblings = new LinkedNodes[linkedNodeObjects.Length];

		for (int i = 0; i < linkedNodesIndex.Length; i++) {
			LinkedNodes ln = linkedNodeObjects[i].GetComponent<LinkedNodes>();
			linkedNodesIndex[i] = ln.index;
			NodeSiblings[i] = ln;
		}
	}
	
	void Update () {
		//Draw lines between each connected waypoint
		foreach(GameObject linkedNode in linkedNodeObjects) {
			Debug.DrawLine (transform.position, linkedNode.transform.position,  Color.green);
		}
	}



    

	public float H;	//A* & Greedy
	public float G; //A*
	public float F { get => H + G; } //A*
	public LinkedNodes AStar_Parent; //A*

	//These operators are used by Greedy
	public static bool operator <(LinkedNodes a, LinkedNodes b) => a.H < b.H; 
	public static bool operator >(LinkedNodes a, LinkedNodes b) => a.H > b.H;

	//General
	public static bool operator ==(LinkedNodes a, LinkedNodes b) => a.index == b.index;
	public static bool operator !=(LinkedNodes a, LinkedNodes b) => a.index != b.index;



	public int CompareTo(object obj)
    {
		int r = 0;
		if (this > (obj as LinkedNodes))
			r = 1;
		if (this < (obj as LinkedNodes))
			r = -1;
		return r;
	}
}

[CustomEditor(typeof(LinkedNodes))]
class LinkedData : Editor
{
	void OnSceneGUI()
	{
		
	}
}