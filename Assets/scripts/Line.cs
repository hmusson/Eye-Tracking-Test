using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line : MonoBehaviour {

    float min;
    float max;
    public int speed = 1;
    public int width = 1;
    bool keyDown = false;

    // Use this for initialization
    void Start()
    {
        
       
        min = -width;
        max = width;
    
        //transform.position = new Vector3(0, 0, 4);
       
    }

    // Update is called once per frame
    void Update()
    {
        
        transform.position = new Vector3(Mathf.PingPong(Time.time * speed, max - min) + min, transform.position.y, transform.position.z);
        if(transform.position.x == max || transform.position.x == min) {
        	Debug.Log("min or max at:" + Time.time);
        }

    }
}
