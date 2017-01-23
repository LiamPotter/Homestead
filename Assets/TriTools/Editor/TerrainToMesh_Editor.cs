using UnityEngine;
using System.Collections;
using UnityEditor;
[CustomEditor(typeof(TerrainToMesh))]
public class TerrainToMesh_Editor : Editor {
    private TerrainToMesh t2m;

    [MenuItem("Terrain/Export To Obj...")]
    static void Init()
    {
        TerrainToMesh.terrain = null;
        Terrain terrainObject = Selection.activeObject as Terrain;
        if (!terrainObject)
        {   
            terrainObject = Terrain.activeTerrain;
        }
        if (terrainObject)
        {
            TerrainToMesh.terrain = terrainObject.terrainData;
            TerrainToMesh.terrainPos = terrainObject.transform.position;
        }

        //EditorWindow.GetWindow<ExportTerrain>().Show();
    }

    public override void OnInspectorGUI()
    {
        if (t2m == null)
            t2m = (TerrainToMesh)target;
        //if (!TerrainToMesh.terrain)
        //{
        //    GUILayout.Label("No terrain found");
        //    //if (GUILayout.Button("Cancel"))
        //    //{
        //    //    //EditorWindow.GetWindow<ExportTerrain>().Close();
        //    //}
        //    return;
        //}
        if (t2m.GetComponent<Terrain>() != null)
            TerrainToMesh.terrain = t2m.GetComponent<Terrain>().terrainData;
  
        t2m.saveFormat = (SaveFormat)EditorGUILayout.EnumPopup("Export Format", t2m.saveFormat);

        t2m.saveResolution = (SaveResolution)EditorGUILayout.EnumPopup("Resolution", t2m.saveResolution);

        if (GUILayout.Button("Export"))
        {
            t2m.fileName = EditorUtility.SaveFilePanel("Export .obj file", "", "Terrain", "obj");
            t2m.Export();
        }
    }
}
