using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(Animator))]
public class PlayerHeadControl : MonoBehaviour {
    public GameObject IKLookTarget;
    private Animator thisAnimator;
    void Awake()
    {
        thisAnimator = GetComponent<Animator>();
    }
    void OnAnimatorIK()
    {
        thisAnimator.SetLookAtWeight(1);
        thisAnimator.SetLookAtPosition(IKLookTarget.transform.position);
    }
}
