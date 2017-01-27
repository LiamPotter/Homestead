using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Network;
public class Test :NetworkedMonoBehavior {
    public LayerMask farmMask;
    public Grid grid;

    public GameObject gridIns;
    [NetSync]
    bool testing = false;
    // Use this for initialization
 
    //    grid = FindObjectOfType<Grid>();
    void Start()
    {
       
    }



    // Update is called once per frame
    void Update () {
        //if (!IsOwner)
        //    return;
        if (Input.GetMouseButtonDown(0))
        {
            //Debug.Log("Clicking");
            Ray ray = (Camera.main.ScreenPointToRay(Input.mousePosition));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100, farmMask))
            {
             
                Node nodeToChange = grid.NodeFromWorldPoint(hit.point);
                Debug.Log("is NodeToChange Null? " + ((nodeToChange==null)?"True":"False"));
                Debug.Log("myTile Null? " + ((nodeToChange.myTile == null) ? "True" : "False"));
                nodeToChange.myTile.TillTile();
               // Debug.Log("ChangingTile");
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && !testing)
        {
            Networking.Instantiate("Farm", NetworkReceivers.AllBuffered,  callback: TestingCallBack);
            
            testing = true;
        }
        if (!Networking.PrimarySocket.IsServer && grid == null)
        {
            RPC("GetGridID", NetworkReceivers.Server);
           
            
        }
    }

    public void TestingCallBack(SimpleNetworkedMonoBehavior c)
    {
        
        grid = c.GetComponent<Grid>();
        grid.Setup(Networking.PrimarySocket, true, 6, 5);
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
