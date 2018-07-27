using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleMover : MonoBehaviour {

    private float RotateSpeed = 5f;
    private float Radius = 0.1f;

    private Vector2 _center;
    private float _angle;

    public KeyCode toggle = KeyCode.Space;
    public bool isVisible = false;

	// Use this for initialization
	void Start () {
        gameObject.SetActive(true); 
     
	}
	
	// Update is called once per frame
	void Update () {
        if(Input.GetKeyDown(toggle))
        {
            if(!isVisible)
            {
                isVisible = true;
            }
        }
       
        if(isVisible)
        {
              
        }
        
    }
}
