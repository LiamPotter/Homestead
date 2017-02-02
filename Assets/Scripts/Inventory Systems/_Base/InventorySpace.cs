﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventorySpace : ScriptableObject
{
    public bool SpaceIsFree=true;
    public InvItem ContainedItem;
    public Button spaceUI;
    public int position;
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