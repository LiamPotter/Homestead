using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour {

    
    public bool showingUI;
    public GameObject inventorySpaceUI_Prefab;
    public Image inventoryHolder;
    public PlayerInventory P_Inventory;
    public FirstPersonMovement playerMovement;  
    private InvItem tempItem;
    private GridLayoutGroup gridLayout;
    private GameObject tempGameObject;
    private Button tempButton;
    [Space]
    [Header("Debug")]
    [Space]
    public bool showDebugControls;
    public Button addItemDebug, removeItemDebug, checkInvDebug;
    void Start ()
    {
        addItemDebug.onClick.AddListener(() => AddTheItem());
        removeItemDebug.onClick.AddListener(() => RemoveTheItem());
        gridLayout = inventoryHolder.GetComponentInChildren<GridLayoutGroup>();
    }
    void Update()
    {
        if (playerMovement.thisPlayer.GetButtonDown("Inventory"))
            showingUI = !showingUI;
        if(tempItem==null)
        {
            if (P_Inventory.WantedInventoryItem != null)
                tempItem = P_Inventory.WantedInventoryItem;
        }
        if(showingUI)
        {
            inventoryHolder.gameObject.SetActive(true);
            if (showDebugControls)
            {
                addItemDebug.gameObject.SetActive(true);
                removeItemDebug.gameObject.SetActive(true);
                checkInvDebug.gameObject.SetActive(true);
            }
            else
            {
                addItemDebug.gameObject.SetActive(false);
                removeItemDebug.gameObject.SetActive(false);
                checkInvDebug.gameObject.SetActive(false);
            }
        }
        else
        {
            inventoryHolder.gameObject.SetActive(false);
            addItemDebug.gameObject.SetActive(false);
            removeItemDebug.gameObject.SetActive(false);
            checkInvDebug.gameObject.SetActive(false);
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
    public void CreateInventoryUI()
    {
        if(!gridLayout)
            gridLayout = inventoryHolder.GetComponentInChildren<GridLayoutGroup>();
        for (int i = 0; i < P_Inventory.InventoryDictionary.Keys.Count; i++)
        {
            tempGameObject = Instantiate(inventorySpaceUI_Prefab, gridLayout.transform) as GameObject;
            tempGameObject.transform.localScale = Vector3.one;
            tempButton = tempGameObject.GetComponent<Button>();
            P_Inventory.InventoryDictionary.ElementAt(i).Value.spaceUI = tempButton;
        }
    
    }
    public void UpdateSpaceUI(int key, string itemName,bool remove)
    {
        if (remove)
            ResetUI(P_Inventory.InventoryDictionary.ElementAt(key).Value.spaceUI);
        else
            ChangeUI(P_Inventory.InventoryDictionary.ElementAt(key).Value.spaceUI,itemName);
    }
    private void ResetUI(Button toChange)
    {
        toChange.GetComponentInChildren<Text>().text = "Empty";
        toChange.GetComponentInChildren<Text>().fontStyle = FontStyle.Italic;
    }
    private void ChangeUI(Button toChange,string itemName)
    {
        toChange.GetComponentInChildren<Text>().text = itemName;
        toChange.GetComponentInChildren<Text>().fontStyle = FontStyle.Normal;
    }
}
