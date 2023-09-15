using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonoBehaviour
{
    public float Resource_Count { get => Resource_Count_; set => Resource_Count_ = Mathf.Clamp(value, 0, 100); }
    public float Resource_Count_ = 50;
    public float Max_Resource_Count = 50;

    public float deadCount = 0;

    public float RegenTime = 30;
    private float RegenTimer;

    public int Regen_Per_Step = 5;

    void Start()
    {
        Resource_Count_ = Random.Range(10, 100);
        Max_Resource_Count = Resource_Count_;
    }


    private void Update()
    {
        if (Resource_Count < Max_Resource_Count)
            RegenTimer += Time.deltaTime;

        if(RegenTimer >= RegenTime)
        {
            Resource_Count += Regen_Per_Step;
            RegenTimer = 0;
        }
    }

}
