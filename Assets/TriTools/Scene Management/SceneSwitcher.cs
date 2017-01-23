using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneSwitcher : MonoBehaviour {

    public int desiredSceneID;
    public KeyCode keyToPress;

	void Update ()
    {
        if (Input.GetKey(keyToPress))
        {

            SceneManager.LoadScene(desiredSceneID);
        }
	}
}
