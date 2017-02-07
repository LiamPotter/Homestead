using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class _FarmingPlots : EditorWindow {

    int rows = 0;
    int columns = 0;
    [MenuItem("Farming/CreateFarm")] 
    static void Init()
    {
        GetWindow(typeof(_FarmingPlots));
    }

    void OnGUI()
    {
        GUIContent content = new GUIContent("Rows");
        GUIContent content2 = new GUIContent("Columns");

        float minWidth;
        float maxWidth;

        GUIStyle style = new GUIStyle();
        style.CalcMinMaxWidth(content, out minWidth, out maxWidth);
        style.fixedWidth = minWidth;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 12;
   
        

        GUILayout.BeginHorizontal();
        GUILayout.Label(content);
        
        int.TryParse((GUI.TextField(new Rect(Screen.width / 2 + 20, Screen.height - 30, minWidth, 20), "", Mathf.RoundToInt( minWidth))), out rows);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label(content2);
        int.TryParse((GUILayout.TextField(columns.ToString(), Mathf.RoundToInt(minWidth))), out columns);
        GUILayout.EndHorizontal();



    }

}
