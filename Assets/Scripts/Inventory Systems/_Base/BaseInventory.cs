﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BaseInventory: MonoBehaviour
{
    public int MaxInventorySpace=10;
    public Dictionary<int, InventorySpace> InventoryDictionary = new Dictionary<int, InventorySpace>();
    private InventorySpace tempFindSpace;
    public InvItem WantedInventoryItem;

    void Start()
    {
        for (int i = 0; i < MaxInventorySpace; i++)
        {
            InventoryDictionary.Add(i,ScriptableObject.CreateInstance<InventorySpace>());
            //Debug.Log("Added " + InventoryDictionary.ElementAt(i)+", is free? "+InventoryDictionary.ElementAt(i).Value.SpaceIsFree);
        }
        WantedInventoryItem = ScriptableObject.CreateInstance<InvItem>();
        WantedInventoryItem.Name = "HelloDebugItem";
    }


    public void AddItem(InvItem toAdd)
    {
        tempFindSpace = FindFreeSpace(InventoryDictionary);
        if(tempFindSpace!=null)
            tempFindSpace.AddedItem(toAdd);
        tempFindSpace = null;
    }
    public void RemoveItem(InvItem toRemove)
    {
        tempFindSpace = FindItemToRemove(InventoryDictionary, toRemove);
        if (tempFindSpace != null)
            tempFindSpace.RemovedItem();
        tempFindSpace = null; 
    }
    InventorySpace FindFreeSpace(Dictionary<int,InventorySpace> dictionary)
    {
        var iSpace =
            from s in dictionary.Values
            where s.SpaceIsFree == true
            select s;
        if (iSpace != null)
            return iSpace.First();
        else
        {
            Debug.LogError("No Inventory Space!");
            return null;
        }
    }
    InventorySpace FindItemToRemove(Dictionary<int,InventorySpace> dictionary,InvItem toFind)
    {
        var iSpace =
            from s in dictionary.Values
            where s.ContainedItem == toFind
            select s;
        if (iSpace != null)
            return iSpace.First();
        else
        {
            Debug.LogError("The item you are trying to remove is not in the player's inventory!");
            return null;
        }
    }
    public void CheckInventory()
    {
        for (int i = 0; i < InventoryDictionary.Keys.Count; i++)
        {
            if (InventoryDictionary.ElementAt(i).Value.SpaceIsFree)
                Debug.Log("Inventory Space "+InventoryDictionary.ElementAt(i).Key + " is empty!");
            else
            {
                Debug.Log("Inventory Space " + InventoryDictionary.ElementAt(i).Key + " currently holds " + InventoryDictionary.ElementAt(i).Value.ContainedItem.Name);
            }
        }
    }
}
