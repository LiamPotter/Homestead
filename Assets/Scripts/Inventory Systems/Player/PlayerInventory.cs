using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : BaseInventory
{
    public InventoryUI invUI;
    private bool doneUICreation=false;
    void Awake()
    {
        AddedItemEvent += AddedItemUIUpdate;
        RemovedItemEvent += RemovedItemUIUpdate;
    }
    void OnDisable()
    {
        AddedItemEvent -= AddedItemUIUpdate;
        RemovedItemEvent -= RemovedItemUIUpdate;
    }
    void Update()
    {
        if(!doneUICreation)
        {
            if(WantedInventoryItem!=null)
            {
                CreateUI();
                doneUICreation = true;
            }
        }
    }
    
    void CreateUI()
    {
        invUI.CreateInventoryUI();
        Debug.Log("Creating UI");
    }
    void AddedItemUIUpdate()
    {
        invUI.UpdateSpaceUI(tempFindSpace.position, tempFindSpace.ContainedItem.Name, false);
    }
    void RemovedItemUIUpdate()
    {
        invUI.UpdateSpaceUI(tempFindSpace.position, tempFindSpace.ContainedItem.Name, true);
    }
}
