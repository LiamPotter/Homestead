using UnityEngine;
using System.Collections;

public class DestroyAfterTime : MonoBehaviour {

    public float waitTime;
    private float actualTime;

	void Start ()
    {
        actualTime = waitTime;
	}
	

	void Update ()
    {
        actualTime -= Time.deltaTime;
        if(actualTime<=0)
        {
            Destroy(gameObject);
        }
	}
}
