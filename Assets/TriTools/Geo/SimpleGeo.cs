// The MIT License (MIT)
// Copyright (c) 2016 David Evans @phosphoer

// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

// USAGE: Make a game object and put this script on it. Click the "Start Editing" button in the inspector
// I recommend making a prefab that you can drag into scene
// Make sure you also have SimpleGeoEditor.cs!
[ExecuteInEditMode]
public class SimpleGeo : MonoBehaviour
{
  public float Height = 0.25f;
  public float Resolution = 10.0f;
  public float BevelRadius = 0.1f;
  public List<TilePos> TileMap = new List<TilePos>();

  private Mesh mesh;

  private List<Vector3> hull;

  [Serializable]
  public struct TilePos
  {
    public int x;
    public int y;
  }

  public void AddPoint(Vector3 point)
  {
    point = transform.InverseTransformPoint(point);
    var x = Mathf.RoundToInt(point.x * Resolution);
    var y = Mathf.RoundToInt(point.z * Resolution);

    TilePos pos;
    pos.x = x;
    pos.y = y;
    if (!TileMap.Contains(pos))
      TileMap.Add(pos);

    RebuildMesh();
  }

  public void RemovePoint(Vector3 point)
  {
    point = transform.InverseTransformPoint(point);
    for (var i = 0; i < TileMap.Count; ++i)
    {
      var tilePos = TileMap[i];
      var p = new Vector3(tilePos.x / Resolution, 0, tilePos.y / Resolution);
      if (Vector3.Distance(p, point) < 0.5f)
        TileMap.RemoveAt(i);
    }

    RebuildMesh();

    if (hull.Count < 3)
    {
      DestroyImmediate(gameObject);
    }
  }

  private void SideLoop(List<Vector3> verts, Vector3 center, float bottom, float top, float bottomInset, float topInset)
  {
    center.y = 0;
    Vector3 a, b, aToCenter, bToCenter;

    // Loop sides
    for (var i = 0; i < hull.Count - 1; ++i)
    {
      a = hull[i];
      b = hull[i + 1];
      aToCenter = (center - a).normalized;
      bToCenter = (center - b).normalized;

      verts.Add(a + Vector3.up * bottom + aToCenter * bottomInset);
      verts.Add(a + Vector3.up * top + aToCenter * topInset);
      verts.Add(b + Vector3.up * bottom + bToCenter * bottomInset);

      verts.Add(b + Vector3.up * bottom + bToCenter * bottomInset);
      verts.Add(a + Vector3.up * top + aToCenter * topInset);
      verts.Add(b + Vector3.up * top + bToCenter * topInset);
    }
    // Connect end
    a = hull[hull.Count - 1];
    b = hull[0];
    aToCenter = (center - a).normalized;
    bToCenter = (center - b).normalized;

    verts.Add(a + Vector3.up * bottom + aToCenter * bottomInset);
    verts.Add(a + Vector3.up * top + aToCenter * topInset);
    verts.Add(b + Vector3.up * bottom + bToCenter * bottomInset);
    verts.Add(b + Vector3.up * bottom + bToCenter * bottomInset);
    verts.Add(a + Vector3.up * top + aToCenter * topInset);
    verts.Add(b + Vector3.up * top + bToCenter * topInset);
  }

  public void RebuildMesh()
  {
    if (mesh == null)
      mesh = new Mesh();
    mesh.Clear();

    // Convex hull the points
    //
    var points = new List<Vector3>();
    foreach (var tilePos in TileMap)
      points.Add(new Vector3(tilePos.x / Resolution, 0, tilePos.y / Resolution));
    hull = ConvexHull(points);

    // Build mesh to extrusion height
    //
    var verts = new List<Vector3>();
    var triangles = new List<int>();
    if (hull.Count >= 3)
    {
      // Find center of hull
      var center = Vector3.zero;
      for (var i = 0; i < hull.Count; ++i)
        center += hull[i];
      center /= hull.Count;
      center.x = Mathf.Round(center.x * Resolution) / Resolution;
      center.z = Mathf.Round(center.z * Resolution) / Resolution;

      // Loop sides
      SideLoop(verts, center, 0, Height - BevelRadius, 0, 0);
      if (BevelRadius != 0)
        SideLoop(verts, center, Height - BevelRadius, Height, 0, BevelRadius);

      // Fan top
      Vector3 a, b, aToCenter, bToCenter;
      for (var i = 0; i < hull.Count - 1; ++i)
      {
        a = hull[i];
        b = hull[i + 1];
        aToCenter = (center - a).normalized;
        bToCenter = (center - b).normalized;

        verts.Add(center + Vector3.up * Height);
        verts.Add(b + Vector3.up * Height + bToCenter * BevelRadius);
        verts.Add(a + Vector3.up * Height + aToCenter * BevelRadius);
      }
      // Connect end
      a = hull[0];
      b = hull[hull.Count - 1];
      aToCenter = (center - a).normalized;
      bToCenter = (center - b).normalized;
      verts.Add(center + Vector3.up * Height);
      verts.Add(a + Vector3.up * Height + aToCenter * BevelRadius);
      verts.Add(b + Vector3.up * Height + bToCenter * BevelRadius);
    }

    // Triangles for each 3 vertices
    for (var i = 0; i < verts.Count; ++i)
      triangles.Add(i);

    // Update mesh
    mesh.SetVertices(verts);
    mesh.SetTriangles(triangles, 0);
    mesh.RecalculateNormals();
    mesh.RecalculateBounds();
    mesh.name = gameObject.name;

    var meshFilter = GetComponent<MeshFilter>();
    if (meshFilter == null)
      meshFilter = gameObject.AddComponent<MeshFilter>();
    meshFilter.mesh = mesh;

    var meshCollider = GetComponent<MeshCollider>();
    if (meshCollider == null)
      meshCollider = gameObject.AddComponent<MeshCollider>();
    meshCollider.sharedMesh = mesh;
    meshCollider.convex = true;

    var r = gameObject.GetComponent<MeshRenderer>();
    if (r == null)
    {
      r = gameObject.AddComponent<MeshRenderer>();
    }
  }

  private void Start()
  {
    RebuildMesh();
  }

  private void OnDrawGizmos()
  {
#if UNITY_EDITOR
    if (!SimpleGeoEditor.EditorActive || Selection.activeGameObject == null)
      return;

    foreach (var tilePos in TileMap)
    {
      var pos = new Vector3(tilePos.x / Resolution, 0, tilePos.y / Resolution);
      Gizmos.DrawSphere(transform.TransformPoint(pos), 1.0f / (Resolution * 5));
    }

    if (hull != null && hull.Count > 1)
    {
      for (var i = 0; i < hull.Count; ++i)
      {
        Gizmos.color = Color.cyan;
        if (i > 0)
          Gizmos.DrawLine(transform.TransformPoint(hull[i - 1]), transform.TransformPoint(hull[i]));
        Gizmos.DrawSphere(transform.TransformPoint(hull[i]), 1.0f / (Resolution * 5));
      }
      Gizmos.DrawLine(transform.TransformPoint(hull[hull.Count - 1]), transform.TransformPoint(hull[0]));
    }
#endif
  }

  private static int ConvexTurn(Vector3 p, Vector3 q, Vector3 r)
  {
    var val = (q.x - p.x)*(r.z - p.z) - (r.x - p.x)*(q.z - p.z);
    if (val < 0)
      return -1;
    if (val == 0)
      return 0;
    return 1;
  }

  private static float ConvexDistance(Vector3 p, Vector3 q)
  {
    return Vector3.SqrMagnitude(p - q);
  }

  private static Vector3 ConvexNextPoint(List<Vector3> points, Vector3 p)
  {
    var q = p;
    foreach (var r in points)
    {
      var t = ConvexTurn(p, q, r);
      if (t == -1 || t == 0 && ConvexDistance(p, r) > ConvexDistance(p, q))
        q = r;
    }
    return q;
  }

  private static List<Vector3> ConvexHull(List<Vector3> points)
  {
    var hull = new List<Vector3>();

    if (points.Count < 3)
      return hull;

    var minPoint = points[0];
    foreach (var p in points)
    {
      if (p.x < minPoint.x)
        minPoint = p;
      else if (Mathf.Abs(p.x - minPoint.x) < Mathf.Epsilon && p.z < minPoint.z)
        minPoint = p;
    }

    var nextHullPoint = minPoint;
    hull.Add(nextHullPoint);
    do
    {
      nextHullPoint = ConvexNextPoint(points, nextHullPoint);
      if (Vector3.Distance(nextHullPoint, hull[0]) > Mathf.Epsilon)
        hull.Add(nextHullPoint);
    } while (Vector3.Distance(nextHullPoint, hull[0]) > Mathf.Epsilon && hull.Count < 50);

    return hull;
  }
}
