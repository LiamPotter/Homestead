using UnityEngine;
using System.Collections;
using TriTools;

public class RotateZ : MonoBehaviour {

    public float speed;

	
	void Update ()
    {
        TriToolHub.Rotate(gameObject, TriToolHub.XYZ.Z, speed, true, Space.Self);
	}
}
