using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class W1_Test_B : MonoBehaviour
{
    public void Forward()
    {
        transform.Translate(Vector3.forward * Time.deltaTime * 5.0f);
    }
}
