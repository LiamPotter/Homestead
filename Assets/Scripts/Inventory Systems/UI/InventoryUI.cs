using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour {

    
    public bool showingUI;
    private bool isShowingUI=false;
    public GameObject inventorySpaceUI_Prefab;
    public Image inventoryHolder;
    public PlayerInventory P_Inventory;
    public FirstPersonMovement playerMovement;  
    private InvItem tempItem;
    private GridLayoutGroup gridLayout;
    public GridLayoutGroup hotbarLayout;
    private GameObject tempGameObject;
    //private Button tempButton;
    private List<Button> invButtons = new List<Button>();
    private List<Button> hotbarButtons = new List<Button>();
    private InventorySpace tempSpace;
    private InventorySpace currentSelectedSpace;
    [Space]
    [Header("Debug")]
    [Space]
    public bool showDebugControls;
    public Button addItemDebug, removeItemDebug, checkInvDebug;
    public InfoPanel infoPanel;
    void Start ()
    {
        //addItemDebug.onClick.AddListener(() => AddTheItem());
        //removeItemDebug.onClick.AddListener(() => RemoveTheItem());
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
            ShowUI();
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
            UpdateInfoPanel(currentSelectedSpace);
        }
        else
        {
            HideUI();
            inventoryHolder.gameObject.SetActive(false);
            addItemDebug.gameObject.SetActive(false);
            removeItemDebug.gameObject.SetActive(false);
        }
    }
    void ShowUI()
    {
        if(!isShowingUI)
        {
            inventoryHolder.gameObject.SetActive(true);
            currentSelectedSpace = P_Inventory.InventoryDictionary[0];
            isShowingUI = true;
        }
    }
    void HideUI()
    {
        if(isShowingUI)
        {
            inventoryHolder.gameObject.SetActive(false);
            isShowingUI = false;
        }   
    }
    //void AddTheItem()
    //{
    //    P_Inventory.AddItem(tempItem);
    //}
    //void RemoveTheItem()
    //{
    //    P_Inventory.RemoveItem(tempItem);
    //}
    public void CreateInventoryUI()
    {
        if(!gridLayout)
            gridLayout = inventoryHolder.GetComponentInChildren<GridLayoutGroup>();
        for (int i = 0; i < P_Inventory.InventoryDictionary.Keys.Count; i++)
        {
            tempGameObject = Instantiate(inventorySpaceUI_Prefab, gridLayout.transform) as GameObject;
            tempGameObject.transform.localScale = Vector3.one;
            invButtons.Add(tempGameObject.GetComponent<Button>());
            invButtons[i].name = "InvButton" + i;
            P_Inventory.InventoryDictionary.ElementAt(i).Value.spaceUI = invButtons[i];
        }
        foreach (Button b in invButtons)
        {
            b.onClick.AddListener(delegate { ChangeSelectedSpace(b,P_Inventory.InventoryDictionary); });
        }
        for (int i = 0; i < P_Inventory.HotbarDictionary.Count; i++)
        {
            tempGameObject = Instantiate(inventorySpaceUI_Prefab, hotbarLayout.transform) as GameObject;
            tempGameObject.transform.localScale = Vector3.one;
            hotbarButtons.Add(tempGameObject.GetComponent<Button>());
            hotbarButtons[i].name = "HotbarButton" + i;
            P_Inventory.HotbarDictionary.ElementAt(i).Value.spaceUI = hotbarButtons[i];
        }
        foreach (Button b in hotbarButtons)
        {
            b.onClick.AddListener(delegate { ChangeSelectedSpace(b,P_Inventory.HotbarDictionary); });
        }
    }
    public void UpdateSpaceUI(int key,Dictionary<int,InventorySpace> suppliedDictionary, string itemName,bool remove)
    {
        if (remove)
            ResetUI(suppliedDictionary.ElementAt(key).Value.spaceUI);
        else
            ChangeUI(suppliedDictionary.ElementAt(key).Value.spaceUI,itemName);
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
    private void UpdateInfoPanel(InventorySpace getInfoFrom)
    {
        if (getInfoFrom.ContainedItem)
        {
            infoPanel.itemName.text = getInfoFrom.ContainedItem.Name;
            infoPanel.itemType.text = getInfoFrom.ContainedItem.ThisItemType.ToString();
        }
        else
        {
            infoPanel.itemName.text = "";
            infoPanel.itemType.text = "";
        }
    }
    public void ChangeSelectedSpace(Button pressed,Dictionary<int, InventorySpace> suppliedDictionary)
    {
        Debug.Log("Buttons is "+pressed.name);
        var toFind =
            from s in suppliedDictionary.Values
            where s.spaceUI == pressed
            select s;
        currentSelectedSpace = (InventorySpace)toFind.First();
    }
    void Destroy()
    {
        //addItemDebug.onClick.RemoveListener(() => AddTheItem());
        //removeItemDebug.onClick.RemoveListener(() => RemoveTheItem());
        for (int i = 0; i < invButtons.Count; i++)
        {
            invButtons[i].onClick.RemoveAllListeners();
        }
    }
}
