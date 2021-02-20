using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private InventorySystem.Inventory m_QuickslotInventory;
    [SerializeField] private InventorySystem.Inventory[] m_Bags;

    private void Awake()
    {
        m_QuickslotInventory.OnShiftClickItemCallback.AddListener(OnShiftClickItemInQuickslot);
        Array.ForEach(m_Bags, bag => bag.OnShiftClickItemCallback.AddListener(OnShiftClickItemInInventory));
    }

    public void OnShiftClickItemInQuickslot(InventorySystem.Inventory inventory, InventorySystem.InventoryItem item)
    {
        if(item.Data != null)
        {
            //Put in first bag slot
        }
    }

    public void OnShiftClickItemInInventory(InventorySystem.Inventory inventory, InventorySystem.InventoryItem item)
    {
        if (item.Data == null)
        {
            inventory.SortInventory();
        }
        else
        {
            //Put in available quickslot or first free slot
        }
    }
}
