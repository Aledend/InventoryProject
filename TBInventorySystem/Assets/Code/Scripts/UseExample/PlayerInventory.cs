using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private InventorySystem.Inventory m_QuickslotInventory;
    [SerializeField] public InventorySystem.Inventory[] m_Bags;
}
