using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldItem : MonoBehaviour {


    //[HideInInspector]
    public InvItem thisItem;

    public string ItemName = "Default";

    public InvItem.IType ItemType;
    [HideInInspector]
    public string seedSpecies;
    void Start()
    {
        thisItem.Name = ItemName;
        thisItem.ThisItemType = ItemType;
        thisItem.seedProps.Species = seedSpecies;
    }
    public void AddItemToInventory(PlayerInventory thePlayerInv)
    {
        thePlayerInv.AddItem(thisItem);
        gameObject.SetActive(false);
        transform.parent = thePlayerInv.transform;
    }
    public void InitializeInvItem()
    {
        thisItem = ScriptableObject.CreateInstance<InvItem>();
        if (ItemName.Length>0)
            thisItem.Name = ItemName;
        else thisItem.Name = name;
        thisItem.ThisItemType = ItemType;
        thisItem.seedProps = ScriptableObject.CreateInstance<SeedProperties>();
    }
}
