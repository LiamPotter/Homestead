using System.Collections;
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
    Grid _grid;
    System.Random psuedoRandom;
    private Node currentNode;

    private bool doneCallback;
    // Use this for initialization
    void Start ()
    {
        _grid = GetComponent<Grid>();
        //Debug.Log(Networking.PrimarySocket.IsServer + "Is Server?");
        if (!Networking.PrimarySocket.IsServer)
        {
            Debug.Log(nodes.Count);
            int x = 0;
            foreach (Node node in _grid.grid)
            {
                positions.Add(node.worldPos);
                nodes.Add(node);
                x++; 
            }
            return;
        }
        
        psuedoRandom = new System.Random(seed.GetHashCode());
        foreach (Node node in _grid.grid)
        {
            //Instantiate and scale Accordingl  y
            positions.Add(node.worldPos);
            nodes.Add(node);
            Networking.Instantiate(tile, NetworkReceivers.AllBuffered, callback: MyCallBack);
        }
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
    private void ClientCallback(Node pNode)
    {

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
	}
}
