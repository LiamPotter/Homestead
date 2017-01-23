using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class AfterTimeEvent : MonoBehaviour {

    [SerializeField]
    public float time;
    [HideInInspector]
    public float pTime;

    public bool disableAfterCompletion;

    public UnityEvent desiredEvent;


    // Use this for initialization
    void Awake ()
    {
        SetTime();
    }
	
	// Update is called once per frame
	void FixedUpdate ()
    {
        if (pTime > 0)
            pTime -= Time.deltaTime;
        if (pTime <= 0)
        {
            desiredEvent.Invoke();
            if (disableAfterCompletion)
                enabled = false;
        }
	}
 
    public void SetTime()
    {
        pTime = time;
    }
}
