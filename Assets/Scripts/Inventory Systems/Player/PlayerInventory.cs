using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PlayerInventory : BaseInventory
{
    public int MaxHotbarSpaces = 5;
    public InventoryUI invUI;
    private FirstPersonMovement playerMovement;
    private bool doneUICreation=false,finishedInitializing=false;
    public Dictionary<int, InventorySpace> HotbarDictionary = new Dictionary<int, InventorySpace>();

    public InventorySpace currentSelectedHotbarSpace;

    void Awake()
    {
        AddedItemEvent += AddedItemUIUpdate;
        RemovedItemEvent += RemovedItemUIUpdate;
        InitializeInventory();
        InitializeHotbar();
        currentSelectedHotbarSpace = HotbarDictionary[0];
        CreateUI();
        playerMovement = invUI.playerMovement;
    }

    void OnDisable()
    {
        AddedItemEvent -= AddedItemUIUpdate;
        RemovedItemEvent -= RemovedItemUIUpdate;
    }

    void Update()
    {
        if(playerMovement.thisPlayer.GetButtonDown("Right Click"))
        {
            if(currentSelectedHotbarSpace.ContainedItem!=null)
            {
                currentSelectedHotbarSpace.ContainedItem.UseThisItem();
            }
        }
    }
    protected void InitializeHotbar()
    {
        for (int i = 0; i < MaxHotbarSpaces; i++)
        {
            HotbarDictionary.Add(i, ScriptableObject.CreateInstance<InventorySpace>());
            HotbarDictionary.ElementAt(i).Value.position = i;
            //Debug.Log("Added " + InventoryDictionary.ElementAt(i)+", is free? "+InventoryDictionary.ElementAt(i).Value.SpaceIsFree);
        }
        finishedInitializing = true;
    }
    public void AddToPlayerInventory(InvItem theItem)
    {
        tempFindSpace = FindFreeSpace(HotbarDictionary);
        if (tempFindSpace!=null)
        {
            Debug.Log("Hello Hotbar space is free"); 
            AddItem(theItem, tempFindSpace,HotbarDictionary);
            //return;
        }
        else
        {
            tempFindSpace = FindFreeSpace(InventoryDictionary);
            if (tempFindSpace != null)
            {
                Debug.Log("Hello Inventory space is free");
                AddItem(theItem, tempFindSpace, InventoryDictionary);

            }
            else
            {
                Debug.LogError("No Free Inventory Space!");
                return;
            }
        }
        theItem.UseEvent += delegate { UseItem(theItem); };
    }
    public void RemoveFromPlayerInventory(InvItem theItem, Dictionary<int,InventorySpace> whatDictionary)
    {
        theItem.ClearEventSubscribers();
        RemoveItem(theItem, whatDictionary);
    }
    void CreateUI()
    {
        invUI.CreateInventoryUI();
        Debug.Log("Creating UI");
    }
    void AddedItemUIUpdate(Dictionary<int,InventorySpace> whatDictionary)
    {
        invUI.UpdateSpaceUI(tempFindSpace.position, whatDictionary, tempFindSpace.ContainedItem.Name, false);
    }
    void RemovedItemUIUpdate(Dictionary<int, InventorySpace> whatDictionary)
    {
        invUI.UpdateSpaceUI(tempFindSpace.position, whatDictionary, tempFindSpace.ContainedItem.Name, true);
    }
    public void UseItem(InvItem itemToUse)
    {
        Debug.Log("UR USING " + itemToUse.Name);
    }
   
}
