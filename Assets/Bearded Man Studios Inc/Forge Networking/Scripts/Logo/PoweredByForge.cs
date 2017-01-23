using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PoweredByForge : MonoBehaviour
{
	public bool LightTheme = false;

	public GameObject[] Disabled;
	public Image LogoImage;

	public Sprite LightThemeLogo;

	public string SceNetoLoad = "ForgeQuickStartMenu";

	// Use this for initialization
	IEnumerator Start()
	{
		if (LightTheme)
		{
			Camera.main.backgroundColor = Color.black;
			LogoImage.sprite = LightThemeLogo;
		}

		foreach (GameObject go in Disabled)
			go.SetActive(true);

		yield return new WaitForSeconds(1.5f);

#if UNITY_4_6 || UNITY_4_7
        Application.LoadLevel(SceNetoLoad);
#else
		BeardedManStudios.Network.Unity.UnitySceneManager.LoadScene(SceNetoLoad);
#endif
	}
}
