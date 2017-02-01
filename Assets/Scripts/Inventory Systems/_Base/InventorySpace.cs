using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventorySpace : ScriptableObject
{
    public bool SpaceIsFree=true;
    public InvItem ContainedItem;
    public void AddedItem(InvItem item)
    {
        ContainedItem = item;
        SpaceIsFree = false;
    }
    public void RemovedItem()
    {
        ContainedItem = null;
        SpaceIsFree = true;
    }
}
