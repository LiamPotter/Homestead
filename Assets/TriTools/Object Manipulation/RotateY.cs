using UnityEngine;
using System.Collections;

public class RotateY : MonoBehaviour {
    public float speed;


	void Start ()
    {
	
	}
	
	void Update () {
        transform.Rotate(new Vector3(0, speed * Time.deltaTime, 0));
	}
}
