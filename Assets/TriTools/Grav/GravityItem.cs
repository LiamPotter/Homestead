// David Evans @phosphoer
// Feel free to use this in your commercial projects
// Let me know if you do cause it will make me feel good and stuff 

using UnityEngine;

// USAGE: 
// This is used internally by the GravitySource for tracking, and should not 
// be manually added to any game objects 
public class GravityItem : MonoBehaviour
{
  public Vector3 Up = Vector3.up;
  public int ActiveFieldCount;
  public float CurrentDistance = Mathf.Infinity;
  public GravitySource CurrentGravitySource;
}