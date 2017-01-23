#if !UNITY_WEBGL
using System.IO;
using UnityEditor;
using UnityEngine;

public class ForgeCreateContext
{
	private static string ContextPath(string append = "")
	{
		return AssetDatabase.GetAssetPath(Selection.activeObject) + append;
	}

	private static void Create(string context, string fileName)
	{
		fileName = UnityEditor.EditorUtility.SaveFilePanel("Create New SNMB", ContextPath(), fileName, "");

		string className = Path.GetFileName(fileName).Split('.')[0];

		File.WriteAllText(fileName + ".txt", context.Replace("class NewSimpleNetworkedMonoBehavior", "class " + className));
		File.Move(fileName + ".txt", fileName + ".cs");
		AssetDatabase.Refresh();
	}

	[MenuItem("Assets/Create/Forge Networking/New SimpleNetworkedMonoBehavior")]
	private static void AddSNMB()
	{
		string context = Resources.Load<TextAsset>("SNMB").text;
		Create(context, "NewSimpleNetworkedMonoBehavior");
	}

	[MenuItem("Assets/Create/Forge Networking/New NetworkedMonoBehavior")]
	private static void AddNMB()
	{
		string context = Resources.Load<TextAsset>("NMB").text;
		Create(context, "NewNetworkedMonoBehavior");
	}
}
#endif