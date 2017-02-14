using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BeardedManStudios.Network;
public class FarmTile : NetworkedMonoBehavior {

   
    public Node myNode;

    [NetSync]
    public string matName;

    [NetSync]
    public Vector3 scale;
    public Dictionary<int, Material> materials = new Dictionary<int, Material>();

    public Material[] mats;

    public float timer = 2000;
    private MeshRenderer mR;

    public bool tilled;

    [NetSync]
    public bool seedPlanted = false;

    public string[] seeds;

    public GameObject seedPlace;
    public SO _scriptableObject;

    private GameObject myCurrentSeed;
    // Use this for initialization
    void Awake () {
        //        myNode = null;
        mR = GetComponent<MeshRenderer>();
	}

    protected override void NetworkStart()
    {
        base.NetworkStart();

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

        if (mR.sharedMaterial.name != matName && matName != null)
            mR.material = _scriptableObject.materials[matName];


        if (transform.position != myNode.worldPos)
           //    transform.position = myNode.worldPos;

        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            ChangeTile(matName);
            
            timer = 2;
        }

        if (matName == "TilledDirt")
            tilled = true;
        else
            tilled = false;
    }

    public void TillTile()
    {
        Debug.Log("Tilling Tile");
        
    
    }

    [BRPC]
    public void ChangeTile(string index)
    {
       
        matName = index;
        mR.material = _scriptableObject.materials[matName];


    }
    public void SetMyNode(Node node)
    {
        myNode = node;
    }
}
