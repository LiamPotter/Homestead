using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[System.Serializable]
[CreateAssetMenu]
public class SO : ScriptableObject
{
    [SerializeField]
    public Dictionary<string, Material> materials = new Dictionary<string, Material>();

    public Material[] mats;

    void OnEnable()
    {
        for (int i = 0; i < mats.Length; i++)
        {
            if (!materials.ContainsValue(mats[i]))
                materials.Add(mats[i].name, mats[i]);
            
        }

    }
}



