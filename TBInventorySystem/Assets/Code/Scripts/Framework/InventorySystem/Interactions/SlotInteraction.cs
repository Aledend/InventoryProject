using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace InventorySystem 
{
    public class SlotInteraction : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image m_TargetImage = null;
        [SerializeField] private InventoryData m_InventoryData = null;


        public void BindImage(in Image image)
        {
            m_TargetImage = image;
            m_TargetImage.sprite = m_InventoryData.SlotNormalSprite;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            m_TargetImage.sprite = m_InventoryData.SlotHoverSprite;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            m_TargetImage.sprite = m_InventoryData.SlotNormalSprite;
        }
    }
}
