using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class OnCollisionEnterEvent : MonoBehaviour {


    public bool disableAfterCompletion;

    public string initiaterTag;

    private bool canInitiate;

    public UnityEvent desiredEvent;

    // Use this for initialization
    void Start()
    {
        if (GetComponent<Collider>().isTrigger)
        {
            Debug.LogError("The connected collider MUST NOT be a trigger! ur dum!");
            canInitiate = false;
        }
        else canInitiate = true;
        if (initiaterTag == "")
        {
            initiaterTag = "Player";
        }
    }

    void OnCollisionEnter(Collision coll)
    {
        if (canInitiate)
        {
            if (coll.collider.tag == initiaterTag)
            {
                desiredEvent.Invoke();
                if (disableAfterCompletion)
                    enabled = false;
            }
        }
    }
}
