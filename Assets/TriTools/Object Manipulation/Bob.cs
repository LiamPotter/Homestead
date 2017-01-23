using UnityEngine;
using System.Collections;

public class Bob : MonoBehaviour {

    public enum Axis
    {
        X,Y,Z
    };
    public Axis axisToAffect;
    public float bobAmount;
    public float bobTime;
    private float lerpCompletion;
    private float pBobTime;
    private Vector3 movementVector;

    // Use this for initialization
    void Start ()
    {
        pBobTime = bobTime;
    }
	
	// Update is called once per frame
	void Update ()
    {
        switch (axisToAffect)
        {
            case Axis.X:
                movementVector = Vector3.right;
                break;
            case Axis.Y:
                movementVector = Vector3.up;
                break;
            case Axis.Z:
                movementVector = Vector3.forward;
                break;
            default:
                Debug.LogError("You shouldn't be seeing this! Contact Liam please.");
                break;
        }
        if (pBobTime>0)
            pBobTime -= Time.deltaTime;
    }
}
