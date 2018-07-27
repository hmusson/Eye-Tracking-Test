using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class oscillator : MonoBehaviour
{

    float timeCounter = 0;
    float x;
    float y;
    public float z;
    public float RotateSpeed = 3f;
    public float Radius = 3f;


    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        timeCounter += Time.deltaTime * RotateSpeed;
        x = (Mathf.Cos(timeCounter)) * Radius;
        y = (Mathf.Sin(timeCounter)) * Radius;
        z = 5;
        transform.position = new Vector3(x, y, z);

    }
}
