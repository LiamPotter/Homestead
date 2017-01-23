using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof (Collider))]
public class OnTriggerEnterEvent : MonoBehaviour {

    public bool disableAfterCompletion;

    public string initiaterTag;

    private bool canInitiate;

    public UnityEvent desiredEvent;

	// Use this for initialization
	void Start ()
    {
        if (!GetComponent<Collider>().isTrigger)
        {
            Debug.LogError("The connected collider MUST be a trigger! ur dum!");
            canInitiate = false;
        }
        else canInitiate = true;
        if (initiaterTag == "")
        {
            initiaterTag = "Player";
        }
	}

    void OnTriggerEnter(Collider coll)
    {
        if (canInitiate)
        {
            if (coll.tag == initiaterTag)
            {
                desiredEvent.Invoke();
                if (disableAfterCompletion)
                    enabled = false;
            }
        }
    }

}
