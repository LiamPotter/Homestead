using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class OnTriggerExitEvent : MonoBehaviour {

    public UnityEvent desiredEvent;

    public string initiaterTag;

    private bool canInitiate;
    // Use this for initialization
    void Start()
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

    void OnTriggerExit(Collider coll)
    {
        if (canInitiate)
        {
            if (coll.tag == initiaterTag)
            {
                desiredEvent.Invoke();
            }
        }
    }
}
