using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldItem : MonoBehaviour {


    //[HideInInspector]
    public InvItem thisItem;

    public string itemName = "Default";

    public InvItem.IType ItemType;

    public void AddItemToInventory(PlayerInventory thePlayerInv)
    {
        thePlayerInv.AddItem(thisItem);
        gameObject.SetActive(false);
        transform.parent = thePlayerInv.transform;
        GetComponent<Collider>().enabled = false;
        GetComponent<Renderer>().enabled = false;
    }
        
    public void InitializeInvItem()
    {
        tag = "Item";
        thisItem = ScriptableObject.CreateInstance<InvItem>();
        if (itemName.Length>0)
            thisItem.Name = itemName;
        else thisItem.Name = name;
        thisItem.ThisItemType = ItemType;
        thisItem.seedProps = ScriptableObject.CreateInstance<SeedProperties>();
    }
}
