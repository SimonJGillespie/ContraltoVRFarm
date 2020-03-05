using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Contralto;

public class Delay : MonoBehaviour {
    public Program alto;
    public float delay;
    public float next;
	// Use this for initialization
	void Start () {
        next = Time.time + delay;
	}
	
	// Update is called once per frame
	void Update () {
        if (Time.time > next)
            alto.enabled = true;
	}
}
