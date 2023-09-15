using UnityEngine;
using System.Collections;

public class Laser_Enemy : Projectile {

	// Use this for initialization
	void Start () {

		//Launch Effect
		Instantiate(hitEffect, transform.position, transform.rotation);

		//Launch Audio
		GameObject launchSFX = Instantiate(launchSound, transform.position, transform.rotation) as GameObject;
		launchSFX.transform.parent = this.transform;

		//Set object kill time
		Destroy (this.gameObject, lifeTime);
	}
	
	// Update is called once per frame
	void Update () {
		//Projectile Movement
		transform.position += Time.deltaTime * projectileSpeed * transform.forward;
	}


	void OnTriggerEnter(Collider otherObject){
		if(otherObject.tag == "Player")
        {
			otherObject.GetComponent<Dreadnaught>().health -= damage;
			Instantiate(hitEffect, transform.position, transform.rotation);
			Instantiate(hitSound, transform.position, transform.rotation);
			Destroy(this.gameObject);
			return;
		}

		if (otherObject.tag == "Environment") {
			otherObject.SendMessage ("takeDamage", damage, SendMessageOptions.DontRequireReceiver);
			Instantiate(hitEffect, transform.position, transform.rotation);
			Instantiate(hitSound, transform.position, transform.rotation);
			Destroy (this.gameObject);
		} 
	}
}
