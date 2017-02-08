using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class _FarmingPlots : EditorWindow {

    int rows = 0;
    int columns = 0;
    float nodeRad;
    GameObject recentlyCreated = null;

    private FarmSO vars;
    [MenuItem("Farming/CreateFarm")] 
    static void Init()
    {
        GetWindow(typeof(_FarmingPlots));
    }
    void OnEnable()
    {
        vars = CreateInstance< FarmSO>();
        AssetDatabase.CreateAsset(vars, "Assets/Resources/FarmSo.asset");
    }
    void OnGUI()
    {
        GUIContent content = new GUIContent("Rows");
        GUIContent content2 = new GUIContent("Columns");
        GUIContent content3 = new GUIContent("Node Radius","between 0.1 and 1 for best results");
        GUIContent createButton = new GUIContent("Create");

        float minWidth;
        float maxWidth;


        GUIStyle style = new GUIStyle();
        style.CalcMinMaxWidth(content, out minWidth, out maxWidth);
        style.fixedWidth = minWidth;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 12;
   

        GUILayout.BeginHorizontal();

        GUILayout.Label(content);
        int.TryParse((GUILayout.TextField(rows.ToString(), Mathf.RoundToInt(minWidth))), out rows);

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        GUILayout.Label(content2);
        int.TryParse((GUILayout.TextField(columns.ToString(), Mathf.RoundToInt(minWidth))), out columns);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        nodeRad = EditorGUILayout.FloatField(content3, nodeRad);
        GUILayout.EndHorizontal();



        if (GUILayout.Button(createButton))
        {
            GameObject newFarm =  AssetDatabase.LoadAssetAtPath("Assets/Perfabs/Farm/Farm.prefab", typeof(GameObject)) as GameObject;
            GameObject farmIns = Instantiate( newFarm,FindObjectOfType<FarmHolder>().transform) as GameObject;
            farmIns.GetComponent<Grid>().gridWorldSize = new Vector2(rows, columns);
            farmIns.GetComponent<Grid>().nodeRadius = nodeRad;
            farmIns.name = "Farming Plot " + FindObjectOfType<FarmHolder>().farms.Count;
            vars.farms.Add(farmIns);
            recentlyCreated = farmIns;
           
            FindObjectOfType<FarmHolder>().farms.Add(farmIns);
            SpawnTiles();
            Close();
        }


    

  
    }

    void SpawnTiles()
    {
        recentlyCreated.GetComponent<Grid>().CreateGrid();
        Debug.Log("CreatingGrid");
    }

   

}
