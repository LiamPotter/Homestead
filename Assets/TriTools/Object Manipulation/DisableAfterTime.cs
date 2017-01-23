using UnityEngine;
using System.Collections;

public class DisableAfterTime : MonoBehaviour {

    public float waitTime;
    private float actualTime;

	void Awake ()
    {
        actualTime = waitTime;
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (gameObject.activeSelf)
        {
            actualTime -= Time.deltaTime;
            if (actualTime <= 0)
            {
                gameObject.SetActive(false);
                enabled = false;
            }
        }
	}
}
