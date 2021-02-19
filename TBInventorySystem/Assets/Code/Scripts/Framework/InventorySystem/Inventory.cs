using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace InventorySystem
{

    [System.Serializable]
    public struct ItemSlot
    {
        Vector2 gridPos;

    }

    /// <summary>
    /// Apply as a serialized variable inside a MonoBehavior to access.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryObject", menuName = "ScriptableObjects/InventorySystem/InventoryObject")]
    public class Inventory : ScriptableObject
    {
        public int Rows = 5;
        public int Cols = 5;

        public float SlotWidth = 15f;
        public float Padding = 3f;
        public float Headerheight = 10f;

        public KeyCode InventoryToggle;
        public bool HideOnPlay = false;
        
        public bool DrawHeader = false;
        public RectTransform UIDragTarget = null;

        public RectTransform UIParent = null;
        public GameObject UIObject = null;
        public InventoryData m_InventoryData;


        //not used
        public ItemSlot[,] Grid;

        // Handles showing and hiding of UI
        #region InputHandling
        // Toggling function applied to the given input listener in OnBeforeSceneLoadRuntimeMethod
        private void ToggleActive()
        {
            if(UIObject)
            {
                UIObject.SetActive(!UIObject.activeSelf);
            }
        }

        // Static list to keep track of all instances of Inventory
        private static readonly List<Inventory> s_InventoryInstances = new List<Inventory>();
        private void OnEnable()
        {
            if(!s_InventoryInstances.Contains(this))
            {
                s_InventoryInstances.Add(this);
            }
            Grid = new ItemSlot[Rows, Cols];
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            s_InventoryInstances.Remove(this);
        }
#endif

        // Called on scene load to imitate an "Awake" function.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod()
        {
            if (Application.isPlaying)
            {
                GameObject inputListener = new GameObject("InventoryInputListener");

                foreach(Inventory inv in s_InventoryInstances)
                {
                    SimpleInputListener sil = inputListener.AddComponent<SimpleInputListener>();
                    sil.Callback += inv.ToggleActive;
                        sil.BoundKey = inv.InventoryToggle;

                    if (inv.HideOnPlay && inv.UIObject)
                    {
                        inv.UIObject.SetActive(false);
                    }
                }
                
            }
        }
        #endregion // Input Handling

        // Handles generating and destroying of UI
        #region InventoryUI
        public void RegenerateUI()
        {
            DestroyUI();
            GenerateUI();
        }

        public void DestroyUI()
        {
            if (Application.isPlaying)
            {
                Destroy(UIObject);
            }
            else
            {
                DestroyImmediate(UIObject);
            }
        }

        public void GenerateUI()
        {
            GameObject background = m_InventoryData.CreateBackground(
                UIParent ? UIParent : m_InventoryData.InventoryCanvas.transform);

            Vector2 innerSize = new Vector2(Cols, Rows) * SlotWidth
                + new Vector2(Cols, Rows) * Padding;
            Vector2 backgroundSize = innerSize + Vector2.one * Padding;

            if (DrawHeader)
            {
                backgroundSize += Vector2.up * Headerheight;
            }

            background.GetComponent<RectTransform>().sizeDelta = backgroundSize;
            Image m_BackgroundImage = background.GetComponent<Image>();

            if (DrawHeader)
            {
                GameObject header = m_InventoryData.CreateHeader(background.transform);
                Vector2 headerPosition = Vector2.up * (backgroundSize.y) * 0.5f;
                headerPosition -= Vector2.up * Headerheight * 0.5f;

                Vector2 headerSize = backgroundSize;
                headerSize.y = Headerheight;

                header.GetComponent<RectTransform>().anchoredPosition = headerPosition;
                header.GetComponent<RectTransform>().sizeDelta = headerSize;
                header.AddComponent<HeaderInteraction>().BindDragTarget(
                    UIDragTarget ? UIDragTarget : m_BackgroundImage.rectTransform);
            }



            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Cols; x++)
                {
                    GameObject slot = m_InventoryData.CreateSlot(background.transform);

                    slot.GetComponent<RectTransform>().sizeDelta = Vector2.one * SlotWidth;

                    Vector2 slotPosition;

                    slotPosition.x = ((float)x / Cols) * innerSize.x;
                    slotPosition.y = ((float)y / Rows) * innerSize.y;

                    slotPosition -= innerSize * 0.5f;
                    slotPosition += Vector2.one * SlotWidth * 0.5f;
                    slotPosition += Vector2.one * Padding * 0.5f;

                    if (DrawHeader)
                        slotPosition -= Vector2.up * Headerheight * 0.5f;

                    slot.GetComponent<RectTransform>().anchoredPosition = slotPosition;

                    slot.GetComponent<SlotInteraction>().BindImage(slot.GetComponent<Image>());
                }
            }

            UIObject = background;
            
        }
        #endregion


        //Serialize content?

        //Resize bag?
    }


}