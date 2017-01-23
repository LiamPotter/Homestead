using UnityEngine;
using UnityEditor;
using System.Collections;

public class SceneOpenerPopup : PopupWindowContent{

    public enum Theme
    {
        Default,
        Dark
    }

    public Theme windowTheme;

    public override Vector2 GetWindowSize()
    {
        return new Vector2(300, 50);
    }

    public override void OnGUI(Rect rect)
    {
        GUIContent content0 = new GUIContent(); //Background GUIContent
        GUIContent content1 = new GUIContent(); //Enum GUIContent
        content1.text = "Window Theme: ";
        GUIStyle style0 = new GUIStyle(); //default text style
        GUIStyle style1 = new GUIStyle(); //bold text style
        style0.padding.left = 10;
        style0.padding.top = 2;
        style1.fontStyle = FontStyle.Bold;
        style1.padding.left = 5;
        style1.padding.top = 5;
        style1.padding.bottom = 5;

        content0.image = (Texture)AssetDatabase.LoadAssetAtPath("Assets/Gizmos/TriTools/greyBackground.png", typeof(Texture));

        content1.tooltip = "Editor Color Scheme.";
        if (windowTheme == Theme.Dark)
        {
            GUI.Box(new Rect(-10, -10, 500, 500), content0.image);//background color
            style0.normal.textColor = Color.white;
            style1.normal.textColor = Color.white;
        }
        else
        {
            style0.normal.textColor = Color.black;
            style1.normal.textColor = Color.black;
        }
        GUILayout.Label("Options", style1);
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(content1, style0);
        windowTheme = (Theme)EditorGUILayout.EnumPopup(GUIContent.none,windowTheme,GUILayout.MaxWidth(180f));
        EditorGUILayout.EndHorizontal();

        editorWindow.Repaint();

        //toggle1 = EditorGUILayout.Toggle("Toggle 1", toggle1);
        //toggle2 = EditorGUILayout.Toggle("Toggle 2", toggle2);
        //toggle3 = EditorGUILayout.Toggle("Toggle 3", toggle3);
    }

    public override void OnOpen()
    {
        editorWindow.Repaint();
    }

    public override void OnClose()
    {
        editorWindow.Repaint();
    }
}
