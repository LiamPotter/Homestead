using UnityEngine;
using System.Collections;
using System;
using BeardedManStudios.Network;

[System.Serializable]
public class Node   {

    public bool walkable;
    public Vector3 worldPos;
    //F cost = G + H
    public int gCost ;
    public int hCost;
    public int movementPenalty;

    public int gridX;
    public int gridY;
    int heapIndex;
    public Node parent;

    public FarmTile myTile;
 

    //public void ChangeTile()
    //{
    //    myTile.TillTile();
    //}
    public Node()
    {

    }

    public  Node(bool _walkable, Vector3 _worldPos, int _gridX, int _gridY,int _penalty)
    {

        walkable = _walkable;
        worldPos = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
        movementPenalty = _penalty; 
        
    }

    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }
    public int HeapIndex
    {
        get
        {
            return heapIndex;
        }
        set
        {
            heapIndex = value;
        }
    }
    public int CompareTo(Node nodeToCompare)
    {
        //Check the F costs if they are equal use the hCost to check 
        int compare = fCost.CompareTo(nodeToCompare.fCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }
        //return negative compare because we want to return 1 if it is lower
        return -compare;
    }

}
