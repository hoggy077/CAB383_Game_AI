using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Player : NavigationAgent {

    //Movement Variables
    public float moveSpeed = 10.0f;
	public float minDistance = 0.1f;

	//Mouse Clicking
	private Vector3 mousePosition;



	

	void Update () {

		//Left Click - Move via Greedy
		if (Input.GetMouseButtonDown (0)) {			
			bool isAccessible = GreedySearch(currentNode, findClosestLinkedNode(), out PathingResults);
			//Debug.Log($"Reached Greedy | Accessible: {isAccessible}, Path Length: {PathingResults.Count}");
			if(isAccessible)
				currentPath = Path2Vector(PathingResults);
		}

		//Right Click - Move via A*
		else if (Input.GetMouseButtonDown (1)) {

			bool isAccessible = AStarSearch(currentNode, findClosestLinkedNode(), out PathingResults);
			//Debug.Log($"Reached AStar | Accessible: {isAccessible}, Path Length: {PathingResults.Count}");
			if (isAccessible)
				currentPath = Path2Vector(PathingResults);
		}
	
		//Move player
		if (currentPath.Count > 0) {
			transform.position = Vector3.MoveTowards(transform.position, currentPath[0], moveSpeed * Time.deltaTime);

			if (Vector3.Distance(transform.position, currentPath[0]) <= minDistance)
			{
				currentNodeIndex = PathingResults[0].index;
				currentPath.RemoveAt(0);
				PathingResults.RemoveAt(0);
			}
		}
    }


	//Find the waypoint that is the closest to where we have clicked the mouse
	private int findClosestWaypoint(){

		//Get mouse coordinates to world position
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition); 
		RaycastHit hit;

		if(Physics.Raycast(ray, out hit)){
			mousePosition = hit.point;
		}

		Debug.DrawLine (Camera.main.transform.position, mousePosition, Color.green);

		float distance = 1000.0f;
		int closestWaypoint = 0;

		//Find the waypoint closest to this position
		for (int i = 0; i < graphNodes.graphNodes.Length; i++) {
			if (Vector3.Distance (mousePosition, graphNodes.graphNodes[i].transform.position) <= distance){
				distance = Vector3.Distance (mousePosition, graphNodes.graphNodes[i].transform.position);
				closestWaypoint = i;
			}
		}

		//print ("Closest Waypoint: " + closestWaypoint);
		
		return closestWaypoint;
	}

	//Get the LinkedNode of the closest waypoint to user input
	private LinkedNodes findClosestLinkedNode() => graphNodes.graphNodes[findClosestWaypoint()].GetComponent<LinkedNodes>();
}


