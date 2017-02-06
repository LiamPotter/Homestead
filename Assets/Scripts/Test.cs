using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Network;
public class Test :NetworkedMonoBehavior {
    public LayerMask farmMask;
    public Grid grid;
    bool gridMade = false;
    public GameObject gridIns;
    [NetSync]
    bool testing = false;

    public GameObject reticle;
    // Use this for initialization
 
    //    grid = FindObjectOfType<Grid>();
    void Start()
    {


    }
  
    private void DrawRayFromCenter()
    {
     

    }
    [BRPC]
    public void PlantSeed(uint tileNum)
    {

    }

    [BRPC]
    public void FindNode(Vector3 worldPos, bool planting)
    {

        Node nodeToChange = grid.NodeFromWorldPoint(worldPos);
        Debug.Log(nodeToChange);
        if (planting)
            nodeToChange.myTile.RPC("SeedPlanted");
        else
            nodeToChange.myTile.TillTile();
        

       // RPC("AllowChange", nodeToChange, NetworkReceivers.OthersProximity);
    }
    public bool CheckPlantable(Vector3 hitPoint)
    {
        Node nodeToChange = grid.NodeFromWorldPoint(hitPoint);

        if (nodeToChange.myTile.tilled)
            return true;
        else
            return false;
    }
    
    public void ChangeShit(Vector3 hitPoint)
    {
        

        Vector3 hitpos = hitPoint;
        if (!Networking.PrimarySocket.IsServer)
        {
            RPC("FindNode", NetworkReceivers.Server, hitpos, false);
            return;
        }

        Node nodeToChange = grid.NodeFromWorldPoint(hitPoint);

        nodeToChange.myTile.TillTile();
    }

    public void PlantShit(Vector3 hitPoint)
    {
        Vector3 hitpos = hitPoint;
        if (!Networking.PrimarySocket.IsServer)
        {
            RPC("FindNode", NetworkReceivers.Server, hitpos, true);
            return;
        }


        Node nodeToChange = grid.NodeFromWorldPoint(hitPoint);

        if (nodeToChange.myTile.tilled)
            nodeToChange.myTile.RPC("SeedPlanted");
    }


    public void InstantiateFarm(Vector3 hitPoint)
    {
        Networking.Instantiate("Farm", hitPoint + Vector3.up * 1.5f, Quaternion.identity, NetworkReceivers.AllBuffered, callback: TestingCallBack);

    }
    // Update is called once per frame
    void Update () {

  
    }
    [BRPC]
    void SetClientGrids(ulong id)
    {
       grid = Locate(id).GetComponent<Grid>();

    }
    public void TestingCallBack(SimpleNetworkedMonoBehavior c)
    {

        grid = c.GetComponent<Grid>();
        grid.gridStartPos = c.transform.position;
        RPC("SetClientGrids", NetworkReceivers.OthersBuffered, grid.NetworkedId);
        Debug.Log(grid.NetworkedId);
    }


   


}
