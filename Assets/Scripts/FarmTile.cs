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

    public float timer = 2000;
    private MeshRenderer mR;

    public bool tilled;

    [NetSync]
    public bool seedPlanted = false;

    public string[] seeds;

    public GameObject seedPlace;

    private GameObject myCurrentSeed;
    // Use this for initialization
    void Awake () {
        //        myNode = null;
        
        mR = GetComponent<MeshRenderer>();
	}

    [BRPC]
    public void SeedPlanted(int seedIndex)
    {
        if (tilled)
        {
            if (!seedPlanted)
            {
                seedPlanted = true;
                Networking.Instantiate(seeds[seedIndex], seedPlace.transform.position, Quaternion.identity, NetworkReceivers.AllBuffered, callback: PlantedSeed);
            }
            else
                Debug.Log("You Have Alread Planted A Seed Silly");
        }
        else
            Debug.Log("You Have To Till The Dirt Silly");



    }

    public void PlantedSeed(SimpleNetworkedMonoBehavior c)
    {
        Debug.Log("You Planted" + c.name + " Seed");
        myCurrentSeed = c.gameObject;
    }

    // Update is called once per frame
    protected override void UnityUpdate() {
        base.UnityUpdate();

        if (scale != Vector3.zero)
            transform.localScale = scale;
        if (mR.material != mats[matIndex])
            mR.material = mats[matIndex];
        if (transform.position != myNode.worldPos)
            transform.position = myNode.worldPos;

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            ChangeTile(matIndex - 1);
            
            timer = 2;
        }

        if (matIndex == 2)
            tilled = true;
        else
            tilled = false;
    }

    public void TillTile()
    {
        Debug.Log("Tilling Tile");
        RPC("ChangeTile",NetworkReceivers.AllBuffered, 2);
    }

    [BRPC]
    public void ChangeTile(int index)
    {
       
        matIndex = index;
        matIndex = Mathf.Clamp(matIndex, 0, 2);
    }
    public void SetMyNode(Node node)
    {
        myNode = node;
    }
}
