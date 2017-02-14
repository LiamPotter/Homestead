using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Network;
public class Test :NetworkedMonoBehavior {
    public LayerMask farmMask;
    bool gridMade = false;
    public GameObject gridIns;
    [NetSync]
    bool testing = false;

    public GameObject reticle;
    private bool x = false;
    void Start()
    {


    }

    [BRPC]
    public void FindNode(Vector3 worldPos, bool planting, ulong id)
    {

        Node nodeToChange = Locate(id).GetComponent<Grid>().NodeFromWorldPoint(worldPos);

        if (planting)
            nodeToChange.myTile.RPC("SeedPlanted", NetworkReceivers.AllBuffered, 0);
        else
            nodeToChange.myTile.RPC("ChangeTile", NetworkReceivers.AllBuffered, "TilledDirt");


        // RPC("AllowChange", nodeToChange, NetworkReceivers.OthersProximity);
    }

    [BRPC]
    private void ClientCheckPlanting(Vector3 worldPos)
    {
        //Node nodeToChange = grid.NodeFromWorldPoint(worldPos);
        //if (nodeToChange.myTile.tilled)
        //{
        //    x = true;
        //}
        //else
        //{
        //    x = false;
        //}
    }
    public void CheckPlantable(Vector3 hitPoint)
    {
        if (!Networking.PrimarySocket.IsServer)
        {
           RPC("ClientCheckPlanting", NetworkReceivers.Server, hitPoint);
           
        }
        else
        {
           // Node nodeToChange = grid.NodeFromWorldPoint(hitPoint);

  
        }
     
    }
    
    public void ChangeShit(Vector3 hitPoint, ulong id)
    {
        Vector3 hitpos = hitPoint;
        if (!Networking.PrimarySocket.IsServer)
        {
            RPC("FindNode", NetworkReceivers.Server, hitpos, false, id);
            return;
        }

        Node nodeToChange = Locate(id).GetComponent<Grid>().NodeFromWorldPoint(hitPoint);

        nodeToChange.myTile.RPC("ChangeTile", NetworkReceivers.AllBuffered, "TilledDirt");
    }

    public void PlantShit(Vector3 hitPoint, ulong id)
    {
        Vector3 hitpos = hitPoint;
        
        if (!Networking.PrimarySocket.IsServer)
        {
            RPC("FindNode", NetworkReceivers.Server, hitpos, true, id);
            return;
        }
        Grid g =  Locate(id).GetComponent<Grid>() as Grid;
        Node nodeToChange = g.NodeFromWorldPoint(hitPoint);

        if (nodeToChange.myTile.tilled)
            nodeToChange.myTile.RPC("SeedPlanted", NetworkReceivers.AllBuffered, 0);

    }


    // Update is called once per frame
 
    


   


}
