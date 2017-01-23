using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneReloader : MonoBehaviour {

    public KeyCode keyToPress;


	
	void Update ()
    {
        if (Input.GetKey(keyToPress))
        {

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
