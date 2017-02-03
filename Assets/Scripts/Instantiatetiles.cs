﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using BeardedManStudios.Network;

public class Instantiatetiles :  NetworkedMonoBehavior{

    public Material[] tileTypes;
    public GameObject tile;
    public string seed;
    public List<FarmTile> tiles = new List<FarmTile>();
    public List<Vector3> positions = new List<Vector3>();
    public List<Node> nodes = new List<Node>();
    public Grid _grid;
   
    System.Random psuedoRandom;
    private Node currentNode;

    private bool done = false;
    private bool doneCallback;
    // Use this for initialization
    protected void Start()
    {
        if (!Networking.PrimarySocket.IsServer)
        {
            RPC("GetGridID", NetworkReceivers.Server);
            return;
        }

        psuedoRandom = new System.Random(seed.GetHashCode());
        _grid = FindObjectOfType<Test>().grid;
        foreach (Node node in _grid.grid)
        {

            positions.Add(node.worldPos);
            nodes.Add(node);
            Networking.Instantiate(tile, NetworkReceivers.AllBuffered, callback: MyCallBack);
        }
    }
    
     

    [BRPC]
    public void GetGridID()
    {
        RPC("SetGridID", OwningNetWorker, NetworkReceivers.OthersBuffered, _grid.NetworkedId);
    }

    [BRPC]
    private void SetGridID(ulong id)
    {

        _grid = Locate(id).GetComponent<Grid>();
    }

    private void MyCallBack(SimpleNetworkedMonoBehavior obj)
    {
        GameObject tile = obj.gameObject;
        //tile.transform.localScale = Vector3.one *( _grid.nodeRadius * 0.2f);
    
        FarmTile thisFTIle = tile.GetComponent<FarmTile>();
        thisFTIle.scale = Vector3.one * (_grid.nodeRadius * 0.2f);

        int x = psuedoRandom.Next(0, tileTypes.Length - 1);
        thisFTIle.RPC("ChangeTile", x);

        thisFTIle.mats = tileTypes;

        tiles.Add(thisFTIle);
   
        // Debug.Log("New Tile created" + obj);
        doneCallback = true;
    }
  

    [BRPC]
    private void GetID(int z)
    {
        RPC("SetID", OwningNetWorker, NetworkReceivers.OthersBuffered,z, tiles[z].NetworkedId);
    }
    [BRPC]
    private void SetID(int z, ulong Tileid)
    {
        tiles.Add(  Locate(Tileid).GetComponent<FarmTile>());
        nodes[z].myTile = tiles[z];
        tiles[z].myNode = nodes[z];
    }
    // Update is called once per frame
    void Update () {
        if (doneCallback && IsServerOwner)
        {

            for (int i = 0; i < tiles.Count; i++)
            {
                tiles[i].transform.position = positions[i];
                tiles[i].myNode = nodes[i];
                nodes[i].myTile = tiles[i];

            }
        }

        if (!Networking.PrimarySocket.IsServer && _grid != null && !done)
        {
            int x = 0;
            foreach (Node node in _grid.grid)
            {
                nodes.Add(node);
                RPC("GetID", NetworkReceivers.Server, x);
                x++;
            }
            done = true;

        }
    }
}
