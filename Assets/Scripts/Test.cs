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

        if (planting)
            nodeToChange.myTile.RPC("SeedPlanted");
        else
            nodeToChange.myTile.TillTile();
        

       // RPC("AllowChange", nodeToChange, NetworkReceivers.OthersProximity);
    }
    // Update is called once per frame
    void Update () {

        if (Input.GetMouseButtonDown(1))
        {
            int x = Screen.width / 2;
            int y = Screen.height / 2;
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(x, y, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100, farmMask))
            {
               
                Vector3 hitpos = hit.point;
                if (!Networking.PrimarySocket.IsServer)
                {
                    RPC("FindNode", NetworkReceivers.Server, hitpos, true);
                    return;
                }

                
                Node nodeToChange = grid.NodeFromWorldPoint(hit.point);

                if (nodeToChange.myTile.tilled)
                    nodeToChange.myTile.RPC("SeedPlanted");

            }
        }

        if (Input.GetMouseButton(0) && testing)
        {
            //Debug.Log("Clicking");
            int x = Screen.width / 2;
            int y = Screen.height / 2;
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(x, y, 0));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100, farmMask))
            {
                Vector3 hitpos = hit.point;
                if (!Networking.PrimarySocket.IsServer)
                {
                    RPC("FindNode", NetworkReceivers.Server, hitpos, false );
                    return;
                }

                Node nodeToChange = grid.NodeFromWorldPoint(hit.point);

                nodeToChange.myTile.TillTile();

            }
        }

        if (Input.GetMouseButtonDown(0) && !testing)
        {
            int x = Screen.width / 2;
            int y = Screen.height / 2;
            Ray ray = Camera.main.ScreenPointToRay(new Vector3(x, y, 0));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100))
            {
                Networking.Instantiate("Farm", hit.point + Vector3.up * 1.5f,Quaternion.identity, NetworkReceivers.AllBuffered, callback: TestingCallBack);

                testing = true;

            }

        }
        if (!Networking.PrimarySocket.IsServer && grid == null)
        {
            RPC("GetGridID", NetworkReceivers.Server);
           
            
        }
    }

    public void TestingCallBack(SimpleNetworkedMonoBehavior c)
    {
        
        grid = c.GetComponent<Grid>();
        grid.gridStartPos = c.transform.position;
        
    }

    [BRPC]
    public void GetGridID()
    {
        RPC("SetGridID", OwningNetWorker, NetworkReceivers.OthersBuffered, grid.NetworkedId); 
    }

    [BRPC]
    private void SetGridID( ulong id)
    {
       
        grid = Locate(id).GetComponent<Grid>();
    }


}
