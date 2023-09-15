using UnityEngine;
using System.Collections;

public class WaypointGraph : MonoBehaviour {

	public GameObject[] graphNodes;

	// Use this for initialization
	void Start () {

        //graphNodes = GameObject.FindGameObjectsWithTag("node");

		//Assign nodes in array the index that they are in graphNodes array
		for (int i = 0; i < graphNodes.Length; i++) {
			graphNodes[i].GetComponent<LinkedNodes>().index = i;
		}
	}
}
