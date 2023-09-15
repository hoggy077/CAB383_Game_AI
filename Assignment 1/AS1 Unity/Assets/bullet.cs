using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class bullet : MonoBehaviour
{
    public float Speed = 50;
    void Start()
    {
        gameObject.GetComponent<Rigidbody>().AddForce(transform.forward * Speed, ForceMode.Impulse);
    }

    void OnCollisionEnter(Collision col)
    {
        Destroy(gameObject);
    }
}
