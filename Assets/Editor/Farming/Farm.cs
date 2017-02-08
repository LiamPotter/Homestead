using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
[CustomEditor(typeof(FarmHolder))]
public class Farm : Editor {
    public GameObject currentActive = null;
    bool move;
    FarmHolder fHolder;
    string[] farmNames;
    bool addGrid;
    bool editing;

    int rows = 0;
    int columns = 0;
    float nodeRad = 0;
    GameObject recentlyCreated = null;
    void OnEnable()
    {
        fHolder = FindObjectOfType<FarmHolder>();
     
    }
    void OnSceneGUI()
    {
        if (Event.current.type == EventType.MouseDown)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit hit;
      
            if (Physics.Raycast(ray, out hit) && move)
            {
                currentActive.transform.position = hit.point;
                move = false;
            }
            
        }
        Selection.activeGameObject = fHolder.gameObject;
    }

    void ChangeEdit()
    {
        if (editing)
        {
            editing = false;
            return;
        }
        else
        {
            editing = true;
            return;
        }
    }

    void CreateGrid(int rows, int columns, float nodeRad)
    {
        GameObject newFarm = AssetDatabase.LoadAssetAtPath("Assets/Perfabs/Farm/Farm.prefab", typeof(GameObject)) as GameObject;
        GameObject farmIns = Instantiate(newFarm, FindObjectOfType<FarmHolder>().transform) as GameObject;
        farmIns.GetComponent<Grid>().gridWorldSize = new Vector2(rows, columns);
        farmIns.GetComponent<Grid>().nodeRadius = nodeRad;
        farmIns.name = "Farming Plot " + FindObjectOfType<FarmHolder>().farms.Count;
     
        recentlyCreated = farmIns;
        recentlyCreated.GetComponent<Grid>().CreateGrid();
        FindObjectOfType<FarmHolder>().farms.Add(farmIns);
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Add Grid"))
        {
            addGrid = true;
        }

        if (addGrid)
        {
            

            GUILayout.BeginHorizontal();

            rows = EditorGUILayout.IntField("Rows", rows);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            columns = EditorGUILayout.IntField("Columns", columns);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            nodeRad = EditorGUILayout.FloatField("Node Radius", nodeRad);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Create"))
            {
                CreateGrid(rows, columns, nodeRad);
                addGrid = false;
            }
        }
        GUILayout.Label("Farms");
        GUILayout.Space(5);

        GUIStyle style = new GUIStyle();


        for (int i = 0; i < fHolder.farms.Count; i++)
        {
            if (editing && i != fHolder.farms.IndexOf(currentActive))
            {
                continue;
            }

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(fHolder.farms[i].name);
                
            if (GUILayout.Button("Edit") )
            {

                currentActive = fHolder.farms[i];
                ChangeEdit();
                
            }
            if (GUILayout.Button("Remove"))
            {
                DestroyImmediate(fHolder.farms[fHolder.farms.Count - 1]);
                fHolder.farms.RemoveAt((fHolder.farms.Count - 1));
            }
            if (GUILayout.Button("move"))
            {
                currentActive = fHolder.farms[i];
                move = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        if (editing)
        {
            Grid g = currentActive.GetComponent<Grid>();

             rows =(int)g.gridWorldSize.x ;
             columns= (int)g.gridWorldSize.y; 
             nodeRad = g.nodeRadius ;

            GUILayout.BeginHorizontal();
  
            rows = EditorGUILayout.IntField("Rows", rows);
        
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            columns = EditorGUILayout.IntField("Columns", columns);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            nodeRad = EditorGUILayout.FloatField("Node Radius", nodeRad);
            GUILayout.EndHorizontal();

            g.gridWorldSize = new Vector2(rows, columns);
            g.nodeRadius = nodeRad;

        }


    }

}
