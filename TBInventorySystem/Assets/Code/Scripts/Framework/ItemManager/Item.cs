using UnityEngine;

namespace InventorySystem
{
    [System.Serializable]
    public struct Item
    {
        [SerializeField] private uint m_ID;
        [SerializeField] private string Name;
        //[SerializeField] private 
        //        [SerializeField] private ItemType itemType; 
    }
}
