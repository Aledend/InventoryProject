using System;
using UnityEngine;
using InventorySystem;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private Inventory m_QuickslotInventory;
    [SerializeField] private Inventory[] m_Bags;

    public Inventory QuickSlotInventory => m_QuickslotInventory;

    [HideInInspector] public UnityEvent<InventoryItem, int> OnItemSelect;

    private readonly KeyCode[] m_AlphaKeyCodes = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9 };

    private InventoryItem m_HeldItem;
    private int m_QuickslotIndex = 0;
    private Image m_DraggedItemUI = null;
    private InventoryItem m_DraggedItem;
    private Inventory m_DragSource = null;
    private Inventory m_CurrentHoveringInventory = null;
    private int m_CurrentHoveringIndex = 0;
    private int m_DragSourceIndex = -1;
    private bool m_Dragging = false;

    public InventoryItem HeldItem => m_HeldItem;

    private void Awake()
    {
        m_QuickslotInventory.OnMouseDownInventorySlot.AddListener(OnMouseDownQuickslot);
        m_QuickslotInventory.OnMouseEnterInventorySlot.AddListener(OnMouseEnterInventorySlot);
        m_QuickslotInventory.OnMouseExitInventorySlot.AddListener(OnMouseExitInventorySlot);
        Array.ForEach(m_Bags, bag => bag.OnMouseDownInventorySlot.AddListener(OnMouseDownInventory));
        Array.ForEach(m_Bags, bag => bag.OnMouseEnterInventorySlot.AddListener(OnMouseEnterInventorySlot));

    }

    private void Update()
    {
        CheckQuickSlotInput();
        HandleDragging();
    }


    private void HandleDragging()
    {
        if(m_Dragging)
        {
            if (m_DraggedItemUI)
            {
                m_DraggedItemUI.transform.position = Input.mousePosition;
                if (Input.GetMouseButtonUp(0))
                {
                    if(m_CurrentHoveringIndex != -1 && m_CurrentHoveringInventory)
                    {
                        m_CurrentHoveringInventory.SwapItems(m_DragSource, m_DragSourceIndex, m_CurrentHoveringIndex);

                        Destroy(m_DraggedItemUI.transform.parent.gameObject);
                    }
                    Destroy(m_DraggedItemUI.transform.parent.gameObject);
                    m_Dragging = false;
                    m_CurrentHoveringIndex = -1;
                    m_CurrentHoveringInventory = null;
                    m_DragSourceIndex = -1;
                    m_DragSource = null;
                }
            }
        }
    }

    public void OnMouseEnterInventorySlot(Inventory inventory, InventoryItem item, int index)
    {
        if(m_Dragging)
        {
            m_CurrentHoveringInventory = inventory;
            m_CurrentHoveringIndex = index;
        }
    }

    public void OnMouseExitInventorySlot(Inventory inventory, InventoryItem item, int index)
    {
        if (m_Dragging)
        {
            m_CurrentHoveringInventory = null;
            m_CurrentHoveringIndex = -1;
        }
    }


    public bool ConsumeItem(out InventoryItem item)
    {
        if(m_QuickslotInventory.TakeItem(m_QuickslotIndex, out InventoryItem invItem))
        {
            item = invItem;
            return true;
        }
        item = new InventoryItem();
        return false;
    }

    private void CheckQuickSlotInput()
    {
        for(int i = 0; i < m_AlphaKeyCodes.Length; i++)
        {
            if(Input.GetKeyDown(m_AlphaKeyCodes[i]))
            {
                m_QuickslotIndex = i;
                m_HeldItem = m_QuickslotInventory.FetchItem(i);
                OnItemSelect.Invoke(m_HeldItem, i);
            }
        }
    }

    public void OnMouseDownQuickslot(Inventory inventory, InventoryItem item, int index)
    {
        if(Input.GetKey(KeyCode.LeftShift))
        {
            if (item.Data == null)
            {
                inventory.SortInventory();
            }
            else
            {
                PutItemInBag(item, index);
            }
        }
        else
        {
            InventoryItem fetch = inventory.FetchItem(index);
            if (fetch.Data != null)
            {
                GameObject go = new GameObject("Temporary item icon canvas");
                

                Canvas canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 2;

                m_DraggedItemUI = new GameObject("temp").AddComponent<Image>();

                m_DraggedItemUI.sprite = item.Data.ItemSprite;
                m_DraggedItemUI.transform.SetParent(go.transform);
                m_DraggedItemUI.transform.position = Input.mousePosition;
                m_DragSource = inventory;
                m_DragSourceIndex = index;
                m_Dragging = true;
            }
        }
    }

    public void OnMouseDownInventory(Inventory inventory, InventoryItem item, int index)
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (item.Data == null)
            {
                inventory.SortInventory();
            }
            else
            {
                if(m_QuickslotInventory.GetFirstEmptySlot(out int slotIndex))
                {
                    if (m_QuickslotInventory.AddItemToSlot(item, slotIndex))
                    {
                        inventory.RemoveItem(index);
                    }
                }
            }
        }
        else
        {
            InventoryItem fetch = inventory.FetchItem(index);
            if (fetch.Data != null)
            {
                GameObject go = new GameObject("Temporary item icon canvas");


                Canvas canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 2;

                m_DraggedItemUI = new GameObject("temp").AddComponent<Image>();

                m_DraggedItemUI.sprite = item.Data.ItemSprite;
                m_DraggedItemUI.transform.SetParent(go.transform);
                m_DraggedItemUI.transform.position = Input.mousePosition;
                m_DragSource = inventory;
                m_DragSourceIndex = index;
                m_Dragging = true;
            }
        }
    }

    private void PutItemInBag(InventoryItem item, int itemIndex)
    {
        foreach(Inventory inventory in m_Bags)
        {
            if(inventory.GetFirstEmptySlot(out int slotIndex))
            {
                if (inventory.AddItemToSlot(item, slotIndex))
                {
                    m_QuickslotInventory.RemoveItem(itemIndex);
                    break;
                }
            }
        }
    }
}
