// The MIT License (MIT)
// Copyright (c) 2016 David Evans @phosphoer

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[CustomEditor(typeof(SimpleGeo))]
public class SimpleGeoEditor : Editor
{
  static public bool EditorActive;

  private float lastHeight;
  private float lastResolution;
  private float lastBevelRadius;

  private void Awake()
  {
    Undo.undoRedoPerformed += OnUndo;
  }

  private void OnUndo()
  {
    var terrains = FindObjectsOfType<SimpleGeo>();
    for (var i = 0; i < terrains.Length; ++i)
    {
      terrains[i].RebuildMesh();
    }
  }

  public override void OnInspectorGUI()
  {
    base.OnInspectorGUI();

    if (!EditorActive && GUILayout.Button("Start editing"))
    {
      EditorActive = true;
      SceneView.RepaintAll();
    }

    if (EditorActive && GUILayout.Button("Stop editing"))
    {
      EditorActive = false;
      SceneView.RepaintAll();
    }

    if (EditorActive)
    {
      GUILayout.Label("LMB + Drag - Paint geometry");
      GUILayout.Label("RMB + Drag - Erase geometry");
      GUILayout.Label("LMB + Shift + Drag - Raise/lower geometry");
      GUILayout.Label("Control - Select geometry under mouse");
      GUILayout.Label("Control + LMB - Start new geometry at cursor");
    }
  }

  private void OnSceneGUI()
  {
    var geoItem = target as SimpleGeo;

    // Update terrain when properties change
    if (lastResolution != geoItem.Resolution || lastBevelRadius != geoItem.BevelRadius || lastHeight != geoItem.Height)
    {
      geoItem.RebuildMesh();
      lastResolution = geoItem.Resolution;
      lastHeight = geoItem.Height;
      lastBevelRadius = geoItem.BevelRadius;
    }

    // Don't do anything if editor isn't active
    if (!EditorActive)
      return;

    // Boilerplate for preventing default events 
    var controlID = GUIUtility.GetControlID(FocusType.Passive);
    if (Event.current.type == EventType.Layout)
    {
      HandleUtility.AddDefaultControl(controlID);
    }

    // Raycast to edit plane
    var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
    var plane = new Plane(geoItem.transform.up, geoItem.transform.position);
    var hitEnter = 0.0f;
    Vector3 hitPoint = Vector3.zero;
    var raycastHitPlane = plane.Raycast(mouseRay, out hitEnter);
    if (raycastHitPlane)
    {
      hitPoint = mouseRay.origin + mouseRay.direction * hitEnter;
    }

    // Draw 3D GUI
    if (raycastHitPlane)
    {
      var c = Color.white;
      c.a = 0.25f;
      Handles.color = c;

      var scale = Mathf.Max(geoItem.transform.localScale.x, geoItem.transform.localScale.z) / geoItem.Resolution;
      Handles.DrawSolidDisc(hitPoint, Vector3.up, scale);
    }

    // Referesh view on mouse move
    if (Event.current.type == EventType.MouseMove)
    {
      SceneView.RepaintAll();
    }

    // Press escape to stop editing 
    if (Event.current.keyCode == KeyCode.Escape)
    {
      EditorActive = false;
      SceneView.RepaintAll();
      Repaint();
      return;
    }

    // Press control to select existing terrain
    if (Event.current.type == EventType.KeyDown && Event.current.control)
    {
      // Raycast to select an existing terrain
      RaycastHit hitInfo;
      if (Physics.Raycast(mouseRay, out hitInfo))
      {
        var t = hitInfo.collider.GetComponent<SimpleGeo>();
        if (t != null)
        {
          Selection.activeGameObject = t.gameObject;
        }
      }
    }

    // Control click to start new terrain 
    if (Event.current.type == EventType.MouseDown && Event.current.control && Event.current.button == 0)
    {
      // Raycast to start new geo at hit point
      RaycastHit hitInfo;
      if (Physics.Raycast(mouseRay, out hitInfo))
      {
        var geoPrefab = PrefabUtility.GetPrefabParent(Selection.activeGameObject);
        var newGeo = geoPrefab != null ? PrefabUtility.InstantiatePrefab(geoPrefab) as GameObject : Instantiate(geoItem.gameObject);
        newGeo.transform.position = hitInfo.point;
        newGeo.transform.localRotation = Quaternion.identity;
        newGeo.transform.up = hitInfo.normal;
        newGeo.GetComponent<SimpleGeo>().TileMap.Clear();
        newGeo.GetComponent<SimpleGeo>().RebuildMesh();
        Selection.activeGameObject = newGeo.gameObject;
        Undo.RegisterCreatedObjectUndo(newGeo, "New SimpleGeo");
      }
    }

    // Click and drag handler
    if (Event.current.type == EventType.MouseDrag && !Event.current.control)
    {
      // Do nothing on camera orbit
      if (Event.current.alt)
        return;

      // Consume the event so the rest of the editor ignores it
      Event.current.Use();

      if (geoItem != null)
        Undo.RecordObject(geoItem, "SimpleGeo Edit");

      // Raycast the mouse drag
      if (raycastHitPlane)
      {
        // Manipulation during terrain creation
        if (Event.current.button == 0)
        {
          // Hold shift to resize terrain height
          if (Event.current.shift)
          {
            geoItem.Height += Event.current.delta.y * -0.03f;
            geoItem.RebuildMesh();
          }
          // Draw on the terrain
          else
          {
            geoItem.AddPoint(hitPoint);
          }
        }
        // Right click to erase terrain
        else if (Event.current.button == 1)
        {
          geoItem.RemovePoint(hitPoint);
        }
      }
    }

    // End the terrain on mouse release
    if (Event.current.type == EventType.MouseUp)
    {
      EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
  }
}
#endif
