using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour {

    public float MaxHealth = 100;
    public float health = 100;

    public GameObject deathEffect;
    public GameObject deathSound;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}


    public virtual void takeDamage(float dmg) {

        health -= dmg;

        if (health <= 0) {
            //Destroy(this.gameObject);
            onDeath();

            Destroy(this.gameObject);

            Instantiate(deathEffect, transform.position, transform.rotation);
            Instantiate(deathSound, transform.position, transform.rotation);
        }
    }

    public virtual void onDeath()   //Added | this is for the grouping system, so a leader can release the group, or select a new leader if they die
    { }
}
