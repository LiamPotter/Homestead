using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
[CreateAssetMenu]
public class SO : ScriptableObject {
    [SerializeField]
    public GameObject farm;

    public List<int> num = new List<int>();
}

[System.Serializable]
public class AList
{
    public GameObject farm;

    public AList(GameObject g)
    {
        farm = g;
    }

}

