using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour,IEventSystemHandler {

    
    public bool showingUI;
    private bool isShowingUI=false;
    public GameObject inventorySpaceUI_Prefab;
    public Camera GUIcam;
    public EventSystem GUIEventSystem;
    public LayerMask UILayer;
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
    public Color ItemInSpaceColor, EmptySpaceColor, SelectedSpaceColor;
    private Ray guiRay;
    public InfoPanel infoPanel;
    public PointerEventData pointerED;
    [Space]
    [Header("Debug")]
    [Space]
    public bool showDebugControls;
    public Button addItemDebug, removeItemDebug, checkInvDebug;
  
    void Start ()
    {
        //addItemDebug.onClick.AddListener(() => AddTheItem());
        //removeItemDebug.onClick.AddListener(() => RemoveTheItem());
        pointerED = new PointerEventData(GUIEventSystem);
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
            DetectDrag(currentSelectedSpace.spaceUI);
            UpdateInfoPanel(currentSelectedSpace);
            UpdateSpaceColors(P_Inventory.InventoryDictionary);
            UpdateSpaceColors(P_Inventory.HotbarDictionary);
        }
        else
        {
            HideUI();
            inventoryHolder.gameObject.SetActive(false);
            addItemDebug.gameObject.SetActive(false);
            removeItemDebug.gameObject.SetActive(false);
            UpdateHotbar();
            UpdateSpaceColors(P_Inventory.HotbarDictionary);
        }


    }
    void ShowUI()
    {
        if(!isShowingUI)
        {
            inventoryHolder.gameObject.SetActive(true);
            currentSelectedSpace = P_Inventory.InventoryDictionary[0];
            P_Inventory.currentSelectedHotbarSpace.spaceUIImage.color = EmptySpaceColor;
            isShowingUI = true;
        }
    }
    void HideUI()
    {
        if(isShowingUI)
        {
            inventoryHolder.gameObject.SetActive(false);
            currentSelectedSpace = P_Inventory.InventoryDictionary[0];
            
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
            tempGameObject.transform.localPosition= new Vector3(tempGameObject.transform.localPosition.x, tempGameObject.transform.localPosition.y, gridLayout.transform.localPosition.z);
            tempGameObject.transform.rotation = gridLayout.transform.rotation;
            invButtons.Add(tempGameObject.GetComponent<Button>());
            invButtons[i].name = "InvButton" + i;
            P_Inventory.InventoryDictionary.ElementAt(i).Value.spaceUI = invButtons[i];
            P_Inventory.InventoryDictionary.ElementAt(i).Value.spaceUIImage = invButtons[i].GetComponent<Image>();
        }
        foreach (Button b in invButtons)
        {
            b.onClick.AddListener(delegate { ChangeSelectedSpace(b,P_Inventory.InventoryDictionary); });
        }
        for (int i = 0; i < P_Inventory.HotbarDictionary.Count; i++)
        {
            tempGameObject = Instantiate(inventorySpaceUI_Prefab, hotbarLayout.transform) as GameObject;
            tempGameObject.transform.localScale = Vector3.one;
            tempGameObject.transform.localPosition = new Vector3(tempGameObject.transform.localPosition.x, tempGameObject.transform.localPosition.y, gridLayout.transform.localPosition.z);
            tempGameObject.transform.rotation = gridLayout.transform.rotation;
            hotbarButtons.Add(tempGameObject.GetComponent<Button>());
            hotbarButtons[i].name = "HotbarButton" + i;
            P_Inventory.HotbarDictionary.ElementAt(i).Value.spaceUI = hotbarButtons[i];
            P_Inventory.HotbarDictionary.ElementAt(i).Value.spaceUIImage = hotbarButtons[i].GetComponent<Image>();
        }
        foreach (Button b in hotbarButtons)
        {
            b.onClick.AddListener(delegate { ChangeSelectedSpace(b,P_Inventory.HotbarDictionary); });
        }

    }
    public void UpdateSpaceColors(Dictionary<int,InventorySpace> spacesToEffect)
    {
        foreach (InventorySpace iSpace in spacesToEffect.Values)
        {
            if (iSpace.spaceUIImage != null)
            {
                if (iSpace == P_Inventory.currentSelectedHotbarSpace&&!showingUI)
                {
                    iSpace.spaceUIImage.color = SelectedSpaceColor;
                    //return;
                }
                else if (iSpace == currentSelectedSpace)
                    iSpace.spaceUIImage.color = SelectedSpaceColor;
                else
                    iSpace.spaceUIImage.color = EmptySpaceColor;
            }
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
    private void DetectDrag(Button currentButton)
    {
        if (playerMovement.thisPlayer.GetButton("Attack"))
        {
            currentButton.OnPointerEnter(pointerED);
        }
    }
    private void UpdateHotbar()
    {
        if (playerMovement.thisPlayer.GetButtonDown("Hotbar1"))
            P_Inventory.currentSelectedHotbarSpace = P_Inventory.HotbarDictionary[0];
        if (playerMovement.thisPlayer.GetButtonDown("Hotbar2"))
            P_Inventory.currentSelectedHotbarSpace = P_Inventory.HotbarDictionary[1];
        if (playerMovement.thisPlayer.GetButtonDown("Hotbar3"))
            P_Inventory.currentSelectedHotbarSpace = P_Inventory.HotbarDictionary[2];
        if (playerMovement.thisPlayer.GetButtonDown("Hotbar4"))
            P_Inventory.currentSelectedHotbarSpace = P_Inventory.HotbarDictionary[3];
        if (playerMovement.thisPlayer.GetButtonDown("Hotbar5"))
            P_Inventory.currentSelectedHotbarSpace = P_Inventory.HotbarDictionary[4];
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
    void Hello()
    {

    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(guiRay.origin,guiRay.direction*100);
    }

}
