using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine;

public class BaseInventory: MonoBehaviour
{
    public int MaxInventorySpace=10;
    public Dictionary<int, InventorySpace> InventoryDictionary = new Dictionary<int, InventorySpace>();
    [HideInInspector]
    public InventorySpace tempFindSpace;
    public InvItem WantedInventoryItem;
    public delegate void InvDelegate(Dictionary<int,InventorySpace> iSpaceDic);
    public event InvDelegate AddedItemEvent;
    public event InvDelegate RemovedItemEvent;

  
    public void InitializeInventory()
    {
        for (int i = 0; i < MaxInventorySpace; i++)
        {
            InventoryDictionary.Add(i, ScriptableObject.CreateInstance<InventorySpace>());
            InventoryDictionary.ElementAt(i).Value.position = i;
            //Debug.Log("Added " + InventoryDictionary.ElementAt(i)+", is free? "+InventoryDictionary.ElementAt(i).Value.SpaceIsFree);
        }
        WantedInventoryItem = ScriptableObject.CreateInstance<InvItem>();
        WantedInventoryItem.Name = "Debug Item";
        WantedInventoryItem.ThisItemType = InvItem.IType.Seed;
    }

    protected void AddItem(InvItem toAdd,InventorySpace space,Dictionary<int,InventorySpace> suppliedDictionary)
    {
        if (tempFindSpace != null)
        {
            tempFindSpace.AddedItem(toAdd);
            AddedItemEvent.Invoke(suppliedDictionary);
        }
        tempFindSpace = null;
    }
    protected void RemoveItem(InvItem toRemove, Dictionary<int, InventorySpace> suppliedDictionary)
    {
        tempFindSpace = FindItemToRemove(InventoryDictionary, toRemove);
        if (tempFindSpace != null)
        {
            RemovedItemEvent.Invoke(suppliedDictionary);
            tempFindSpace.RemovedItem();
        }
        tempFindSpace = null;
    }
    protected InventorySpace FindFreeSpace(Dictionary<int,InventorySpace> dictionary)
    {
        var iSpace =
            from s in dictionary.Values
            where s.SpaceIsFree == true
            select s;
        iSpace.DefaultIfEmpty(null);
        if (iSpace.FirstOrDefault())
            return iSpace.First();
        else
        {
            Debug.LogWarning("No Inventory Space!");
            return null;
        }
    }
    protected InventorySpace FindItemToRemove(Dictionary<int,InventorySpace> dictionary,InvItem toFind)
    {
        var iSpace =
            from s in dictionary.Values
            where s.ContainedItem == toFind
            select s;
        if (iSpace.Last().ContainedItem==toFind)
            return iSpace.Last();
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
            //Debug.Log("Inventory Space "+ InventoryDictionary.ElementAt(i).Key+" UI is " +InventoryDictionary.ElementAt(i).Value.spaceUI);
            if (InventoryDictionary.ElementAt(i).Value.SpaceIsFree)
                Debug.Log("Inventory Space "+InventoryDictionary.ElementAt(i).Key + " is empty!");
            else
            {
                Debug.Log("Inventory Space " + InventoryDictionary.ElementAt(i).Key + " currently holds " + InventoryDictionary.ElementAt(i).Value.ContainedItem.Name);
            }
        }
    }
}
