using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct ItemSlot
{
    Vector2 gridPos;

}

public class Inventory : MonoBehaviour
{
    public int rows;
    public int cols;

    public ItemSlot[,] grid;
    
    public KeyCode toggle;

    public float slotWidth = 15f;
    public float padding = 15f;
    public float headerheight = 15f;

    public bool drawHeader = false;

    public InventoryData m_InventoryData;

    private Image m_BackgroundImage = null;

    private void OnEnable()
    {
        grid = new ItemSlot[rows, cols];
    }

    public Image BuildInventoryUI()
    {
        GameObject background = m_InventoryData.CreateBackground(m_InventoryData.InventoryCanvas.transform);

        Vector2 innerSize = new Vector2(cols, rows) * slotWidth
            + new Vector2(cols, rows) * padding;
        Vector2 backgroundSize = innerSize + Vector2.one * padding;
            
        if(drawHeader)
        {
            backgroundSize += Vector2.up * headerheight;
        }

        background.GetComponent<RectTransform>().sizeDelta = backgroundSize;
        m_BackgroundImage = background.GetComponent<Image>();

        if (drawHeader)
        {
            GameObject header = m_InventoryData.CreateHeader(background.transform);
            Vector2 headerPosition = Vector2.up * (backgroundSize.y) * 0.5f;
            headerPosition -= Vector2.up * headerheight * 0.5f;

            Vector2 headerSize = backgroundSize;
            headerSize.y = headerheight;

            header.GetComponent<RectTransform>().anchoredPosition = headerPosition;
            header.GetComponent<RectTransform>().sizeDelta = headerSize;
            header.AddComponent<HeaderInteraction>().BindDragTarget(m_BackgroundImage.rectTransform);
        }

        

        for (int y = 0; y < rows; y++)
        { 
            for(int x = 0; x < cols; x++)
            {
                GameObject slot = m_InventoryData.CreateSlot(background.transform);

                Vector2 slotPosition;

                slotPosition.x = ((float)x / cols) * innerSize.x;
                slotPosition.y = ((float)y / rows) * innerSize.y;

                slotPosition -= innerSize * 0.5f;
                slotPosition += Vector2.one * slotWidth * 0.5f;
                slotPosition += Vector2.one * padding * 0.5f;

                if(drawHeader)
                    slotPosition -= Vector2.up * headerheight * 0.5f;

                slot.GetComponent<RectTransform>().anchoredPosition = slotPosition;
            }
        }

        return m_BackgroundImage;
    }
    
    //Serialize content?

    //Resize bag
}
