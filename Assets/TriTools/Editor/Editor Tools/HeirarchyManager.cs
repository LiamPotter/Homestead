using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class HeirarchyManager : MonoBehaviour {

    public string groupName;

    public Transform holderTransform;

    public string findName;

    public List<Transform> nameFounds;

	// Use this for initialization
	void Start ()
    {
        nameFounds = FindWithName(findName);
	}
	
	// Update is called once per frame
	void Update ()
    {

    }
    public List<Transform> FindWithName(string name)
    {
        List<Transform> pList = new List<Transform>();

        foreach (Transform t in FindObjectsOfType<Transform>())
        {
            if (t.name.Contains(name))
                pList.Add(t);
        }

        //Debug.Log("Plist length: " + pList.Count);
        return pList;
    }
}
