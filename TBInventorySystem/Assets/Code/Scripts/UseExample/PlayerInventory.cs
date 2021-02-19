using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private InventorySystem.Inventory m_QuickslotInventory;
    [SerializeField] private InventorySystem.Inventory[] m_Bags;
}
