using UnityEngine;
using System.Collections;

public class CameraController : MonoBehaviour {

	//Mouse Scrolling
	public float scrollDistance = 5.0f;
	public float scrollSpeed = 25.0f;

	//Mouse Zoom
	public float cameraHeight = 15.0f;
	public GameObject camera;

    //Focus
    private Vector3 reset;

	// Use this for initialization
	void Start () {
        reset = transform.position;
	}
	
	// Update is called once per frame
	void Update () {
        Movement ();
    }

	//Move Camera using Mouse
	void Movement(){

		// Horizontal Movement
		if (Input.mousePosition.x < scrollDistance)
			transform.Translate (Vector3.right * -scrollSpeed * Time.deltaTime);
		else if(Input.mousePosition.x >= Screen.width - scrollDistance)
			transform.Translate (Vector3.right * scrollSpeed * Time.deltaTime);

		// Vertical Movement
		if (Input.mousePosition.y < scrollDistance)
			transform.Translate (transform.forward * -scrollSpeed * Time.deltaTime);
		else if(Input.mousePosition.y >= Screen.height - scrollDistance)
			transform.Translate (transform.forward * scrollSpeed * Time.deltaTime);

        //Space - reset camera
        if (Input.GetKey("space")) {
            transform.position = reset;
        }
	}
}
