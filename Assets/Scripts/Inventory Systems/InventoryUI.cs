using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour {

    public Button addItemDebug, removeItemDebug;
    public BaseEventData toDo;
    private PlayerInventory P_Inventory;
    private InvItem tempItem;
	void Start ()
    {
        P_Inventory = FindObjectOfType<PlayerInventory>();
        addItemDebug.onClick.AddListener(() => AddTheItem());
        removeItemDebug.onClick.AddListener(() => RemoveTheItem());
    }
    void Update()
    {
        if(tempItem==null)
        {
            if (P_Inventory.WantedInventoryItem != null)
                tempItem = P_Inventory.WantedInventoryItem;
        }
    }
    void AddTheItem()
    {
        P_Inventory.AddItem(tempItem);
    }
    void RemoveTheItem()
    {
        P_Inventory.RemoveItem(tempItem);
    }
}
