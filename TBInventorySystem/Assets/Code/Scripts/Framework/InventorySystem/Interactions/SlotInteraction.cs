using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventorySystem 
{
    public class SlotInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image m_HoverImage = null;
        [SerializeField] private Image m_ItemImage = null;
        [SerializeField] private InventoryData m_InventoryData = null;
        [SerializeField] private Text m_Text = null;
        [SerializeField] [HideInInspector] private Inventory m_Inventory = null;
        [SerializeField] private GameObject m_HoverTooltip = null;
        [SerializeField] private Text m_HoverName = null;
        [SerializeField] private Text m_HoverDescription = null;

        private GameObject m_DescriptionParent = null;
        private Transform m_OldParent = null;

        public void BindProperties(in Inventory inventory, Sprite itemSprite = null)
        {
            m_HoverImage.sprite = m_InventoryData.SlotEmptySprite;
            m_Inventory = inventory;
            if (itemSprite)
                m_ItemImage.sprite = itemSprite;
        }

        public void SetSprite(in InventoryItem item)
        {
            m_ItemImage.sprite = item.Data.ItemSprite;
            m_Text.text = item.Amount.ToString();
            m_HoverName.text = item.Data.ItemName;
            m_HoverDescription.text = item.Data.Description;
        }

        public void RemoveSprite()
        {
            m_ItemImage.sprite = m_Inventory.m_InventoryData.SlotEmptySprite;
            m_Text.text = string.Empty;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            
            if(Input.GetKey(KeyCode.LeftControl))
            {
                //Amount popup
            }
            else if(Input.GetKey(KeyCode.LeftShift))
            {
                m_Inventory.ShiftClickItem(gameObject);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_HoverImage.sprite = m_InventoryData.SlotHoverSprite;
            m_Inventory.ItemSlotToIndex(gameObject);
            if(m_ItemImage.sprite != m_InventoryData.SlotEmptySprite)
            {
                m_HoverTooltip.SetActive(true);
                m_OldParent = m_HoverTooltip.transform.parent;
                m_DescriptionParent = new GameObject("Temporary Canvas Object");
                Canvas c = m_DescriptionParent.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 2;
                m_HoverTooltip.transform.SetParent(c.transform);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_HoverImage.sprite = m_InventoryData.SlotEmptySprite;

            if(m_DescriptionParent)
            {
                m_HoverTooltip.transform.SetParent(m_OldParent);
                Destroy(m_DescriptionParent.gameObject);
                m_HoverTooltip.SetActive(false);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            //If held item put on this one  
        }
    }
}
