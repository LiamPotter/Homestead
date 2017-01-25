using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Network;
public class FarmTile : NetworkedMonoBehavior {

   
    public Node myNode;
    
    [NetSync]
    public int matIndex = 0;

    [NetSync]
    public Vector3 scale;

    public Material[] mats;


    private MeshRenderer mR;
	// Use this for initialization
	void Awake () {
        //        myNode = null;
        
        mR = GetComponent<MeshRenderer>();
	}

    // Update is called once per frame
    protected override void UnityUpdate() {
        base.UnityUpdate();
        if (scale != Vector3.zero)
            transform.localScale = scale;
        if (mR.material != mats[matIndex])
            mR.material = mats[matIndex];
     
    }

    public void TillTile()
    {
        Debug.Log("Tilling Tile");
        RPC("ChangeTile",NetworkReceivers.AllBuffered, 2);
    }

    [BRPC]
    public void ChangeTile(int index)
    {
        //Debug.Log("ChangingTile" + index);
        matIndex = index;
        //switch (matIndex)
        //{
        //    case 0:
        //        mR.material = mats[matIndex];
        //        break;
        //    case 1:
        //        mR.material = mats[matIndex];
        //        break;
        //    case 2:
        //        mR.material = mats[matIndex];

        //        break;

        //    default:
        //        break;
        //}
    }
    public void SetMyNode(Node node)
    {
        myNode = node;
    }
}
