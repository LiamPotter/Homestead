using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class FarmHolder : MonoBehaviour {


    public List<GameObject> farms = new List<GameObject>();

    public LayerMask farmMask;

    public SO so;

    void Start()
    {
       // farms.AddRange(so.theList);
    }
   

}
