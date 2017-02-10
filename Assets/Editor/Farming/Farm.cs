using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

enum PaintMode
{
    Unwalkable,
    Grass,
    Dirt
}

[System.Serializable]
[CustomEditor(typeof(FarmHolder))]
public class Farm : Editor {
    public GameObject currentActive = null;
    bool move;
    FarmHolder fHolder;
    string[] farmNames;
    bool addGrid;
    bool editing;
    bool clcikToPlace;
    int rows = 0;
    int columns = 0;
    float nodeRad = 0;
    float radius = 0;

    [SerializeField]
    SO so;

    Vector3 fPos;
    SerializedObject _SO;
    Grid gridActive;

    [SerializeField]
    private List<GameObject> farm = new List<GameObject>();

    private List<GameObject> tilesChanging = new List<GameObject>();
    GameObject recentlyCreated = null;
    Vector2 mousePos;
    PaintMode pMode;
    void OnEnable()
    {
        fHolder = FindObjectOfType<FarmHolder>();
        so = (SO)AssetDatabase.LoadAssetAtPath("Assets/Resources/MySO.asset", typeof(SO));
        _SO = new SerializedObject(so);

        EditorUtility.SetDirty(so);
        //farm.AddRange(so.theList);    

    }
    public void OnDisable()
    {
       
        AssetDatabase.SaveAssets();
        _SO.ApplyModifiedProperties();
        
        AssetDatabase.Refresh();
    }
    void OnSceneGUI()
    {
        if(currentActive != null)
             gridActive = currentActive.GetComponent<Grid>();

        if (Event.current.type == EventType.MouseDown)
        {
            if (clcikToPlace)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit) )
                {
                    CreateGrid(rows, columns, nodeRad, hit.point);
                    clcikToPlace = false;
                }

            
            }

            else if(move)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit) && move)
                {
                    currentActive.transform.position = hit.point;
                    currentActive.GetComponent<Grid>().gridStartPos = hit.point;
                    move = false;
                }
            }
           
          Event.current.Use();

        }

   
        if (Event.current.type == EventType.MouseDrag && editing)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            RaycastHit[] hitinfo;
            hitinfo = Physics.SphereCastAll(ray, radius * 0.01f, 150, fHolder.farmMask);


            //Debug.Log(hitinfo.Length);
            foreach (RaycastHit hit in hitinfo)
            {
                if (tilesChanging.Contains(hit.transform.gameObject))
                    continue;

                 tilesChanging.Add(hit.transform.gameObject);
                 ChangeTiles(hit.transform.gameObject);

            }
            Event.current.Use();

        }

        if (Event.current.type == EventType.MouseUp)
        {

            tilesChanging.Clear();
        }

        if(editing)
        {
            Handles.BeginGUI();
            Handles.color = Color.blue;
            Handles.DrawWireCube(Event.current.mousePosition, Vector3.one * radius);
            Handles.EndGUI();
        }
    
     
    }
    void ChangeTiles(GameObject tile)
    {
        switch (pMode)
        {
            case PaintMode.Unwalkable:
                tile.GetComponent<MeshRenderer>().material = Resources.Load("UnWalkable") as Material;
                break;
            case PaintMode.Grass:
                tile.GetComponent<MeshRenderer>().material = Resources.Load("Grass") as Material;
                break;
            case PaintMode.Dirt:
                tile.GetComponent<MeshRenderer>().material = Resources.Load("Dirt") as Material;
                break;
            default:
                break;
        }

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

    void CreateGrid(int rows, int columns, float nodeRad, Vector3 pos)
    {
        GameObject newFarm = AssetDatabase.LoadAssetAtPath("Assets/Perfabs/Farm/Farm.prefab", typeof(GameObject)) as GameObject;
        GameObject farmIns = Instantiate(newFarm, pos, Quaternion.identity, FindObjectOfType<FarmHolder>().transform) as GameObject;
        farmIns.GetComponent<Grid>().gridWorldSize = new Vector2(rows, columns);
        farmIns.GetComponent<Grid>().nodeRadius = nodeRad;
        farmIns.name = "Farming Plot " + farm.Count;
     
        recentlyCreated = farmIns;
        recentlyCreated.GetComponent<Grid>().CreateGrid();

        farm.Add(farmIns);
        //so.theList.Add(farmIns);
        so.num.Add( Random.Range(0, 5));
        EditorUtility.SetDirty(so);
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        _SO.Update();
    
        if (GUILayout.Button("Add Grid"))
        {
            addGrid = true;
            editing = false;
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

            GUILayout.Label("Add Vecter3 Position or Click Create and Click Anywhere In Scene View");
            fPos = EditorGUILayout.Vector3Field("Position ", fPos);

            if (GUILayout.Button("Create"))
            {
                if(fPos == Vector3.zero)
                    clcikToPlace = true;
                else
                    CreateGrid(rows, columns, nodeRad, fPos);

                addGrid = false;
            }
        }
        GUILayout.Label("Farms");
        GUILayout.Space(5);

        GUIStyle style = new GUIStyle();

        
        for (int i = 0; i < farm.Count; i++)
        {
            if (editing && i != farm.IndexOf(currentActive))
            {
                continue;
            }

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(farm[i].name);
                
            if (GUILayout.Button("Edit") )
            {

                currentActive = farm[i];
                ChangeEdit();
                
            }
            if (GUILayout.Button("Remove"))
            {
                DestroyImmediate(farm[farm.Count - 1]);
           
                farm.RemoveAt((farm.Count - 1));
                
                editing = false;
            }
            if (GUILayout.Button("move"))
            {
                currentActive = farm[i];
                editing = false;
                move = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
        }

        if (editing)
        {

            pMode = (PaintMode)EditorGUILayout.EnumPopup("Paint", pMode);
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

            radius = EditorGUILayout.FloatField("Brush Size", radius);

           
        }
        _SO.ApplyModifiedProperties();
        EditorUtility.SetDirty(so);
        ListIterator("theList", _SO, style, "SOME TITLE");
    }

    public void ListIterator(string propertyPath, SerializedObject serializedObject, GUIStyle style, string title)
    {
        SerializedProperty listProperty = serializedObject.FindProperty(propertyPath);
       // visible = EditorGUILayout.Foldout(visible, title, style);
      
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                EditorGUILayout.Space();
                SerializedProperty elementProperty = listProperty.GetArrayElementAtIndex(i);

                farm[i] = GameObject.Find(elementProperty.objectReferenceValue.name);


         
                
            }
        
    }

}
