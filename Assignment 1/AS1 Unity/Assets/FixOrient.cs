using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixOrient : MonoBehaviour
{

    void Start()
    {
        
    }


    void Update()
    {
        transform.rotation = Quaternion.LookRotation(Vector3.forward, transform.forward);
    }
}
