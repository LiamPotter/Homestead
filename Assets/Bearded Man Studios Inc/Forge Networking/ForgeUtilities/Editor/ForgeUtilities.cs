using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class ForgeUtilities
{
	[MenuItem("Window/Forge Networking/Tools/Chat")]
	private static void NewMenuOption()
	{
		GameObject.Instantiate(Resources.Load("FN_ChatWindow"));

		if (GameObject.FindObjectOfType<EventSystem>() == null)
		{
			GameObject evt = new GameObject("Event System");
			evt.AddComponent<EventSystem>();
			evt.AddComponent<StandaloneInputModule>();
#if UNITY_4_6 || UNITY_4_7
			evt.AddComponent<TouchInputModule>();
#endif
		}
	}
}
