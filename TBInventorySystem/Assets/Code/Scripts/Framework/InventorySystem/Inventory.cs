using System;
using System.Collections.Generic;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.Events;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace InventorySystem
{
    using Framework;

    [System.Serializable]
    public struct ItemSlot
    {
        Vector2 gridPos;

    }

    [Serializable]
    public struct Serere
    {
        public RectTransform rect;
    }

    /// <summary>
    /// Apply as a serialized variable inside a MonoBehavior to access.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryObject", menuName = "ScriptableObjects/InventorySystem/InventoryObject")]
    public class Inventory : ScriptableObject, ISerializationCallbackReceiver
    {

        public Serere ser;

        public int Rows = 5;
        public int Cols = 5;

        public float SlotWidth = 15f;
        public float Padding = 3f;
        public float Headerheight = 10f;

        public KeyCode InventoryToggle;
        public bool HideOnPlay = false;
        
        public bool DrawHeader = false;
        public GameObject UIDragTarget = null;

        public GameObject UIParent = null;
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
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeSceneLoadRuntimeMethod;
            AssemblyReloadEvents.afterAssemblyReload += OnBeforeSceneLoadRuntimeMethod;
#endif
            if (!s_InventoryInstances.Contains(this))
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
        public void RegenerateUI(in RectTransform parent = null)
        {
            DestroyUI();
            GenerateUI(parent);
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

        public void GenerateUIGroup(in Inventory[] inventories, in RectTransform parent)
        {
            RectTransform groupParent = m_InventoryData.CreateInventoryGroup(parent).GetComponent<RectTransform>();

            groupParent.offsetMin = Vector2.zero;
            groupParent.offsetMax = Vector2.zero;

            Array.ForEach(inventories, inv => {
                if (inv != null)
                {
                    inv.RegenerateUI(groupParent);
                    RectTransform invTrans = inv.UIObject.GetComponent<RectTransform>();
                    invTrans.anchorMax = new Vector2(1f, 0f);
                    invTrans.anchorMin = new Vector2(1f, 0f);
                }
            });


            float inventoryPadding = 15f;
            Rect latestPosition = CalculateInitialPosition(inventoryPadding, in groupParent);
            PositionUIElement(ref latestPosition);
            float maxWidth = UIObject.GetComponent<RectTransform>().rect.width;


            foreach (Inventory inv in inventories)
            {
                if (inv != this && inv != null)
                {
                    latestPosition = CalculatePosition(inventoryPadding, ref latestPosition,
                        in groupParent, in inv, ref maxWidth);
                    inv.PositionUIElement(ref latestPosition);
                }
            }
        }

        public void GenerateUI(in RectTransform inParentGroup, ref Vector2 latestPosition)
        {
            GenerateUI(inParentGroup);
        }

        private void PositionUIElement(ref Rect latestPosition)
        {
            UIObject.GetComponent<RectTransform>().anchoredPosition = latestPosition.position;
        }

        public void GenerateUI(in RectTransform inParentGroup = null)
        {
            Transform parent = inParentGroup ? inParentGroup
                : UIParent ? UIParent.GetComponent<RectTransform>()
                : m_InventoryData.InventoryCanvas.transform;

            SerializedObject obj = new SerializedObject(this);
            

            GameObject background = m_InventoryData.CreateBackground(parent);


#if UNITY_EDITOR
            InventorySceneReference sceneRef = background.AddComponent<InventorySceneReference>();
            UnityEventTools.AddPersistentListener(sceneRef.ReturnObject, ReturnUIObject);
            sceneRef.ReturnObject.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            if (inParentGroup)
            {
                Debug.Log(inParentGroup.name);
                sceneRef = inParentGroup.gameObject.AddComponent<InventorySceneReference>();
                UnityEventTools.AddPersistentListener(sceneRef.ReturnObject, ReturnUIParent);
                sceneRef.ReturnObject.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
                sceneRef = inParentGroup.gameObject.AddComponent<InventorySceneReference>(); 
                UnityEventTools.AddPersistentListener(sceneRef.ReturnObject, ReturnUIDragTarget); 
                sceneRef.ReturnObject.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            }
            else
            {
                sceneRef = background.AddComponent<InventorySceneReference>();
                UnityEventTools.AddPersistentListener(sceneRef.ReturnObject, ReturnUIDragTarget);
                sceneRef.ReturnObject.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            }
#endif

            Vector2 innerSize = CalculateInnerSize();
            Vector2 backgroundSize = CalculateObjectSize();

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
                if(inParentGroup)
                {
                    UIDragTarget = inParentGroup.gameObject;
                }
                header.AddComponent<HeaderInteraction>().BindDragTarget(
                    UIDragTarget ? UIDragTarget.GetComponent<RectTransform>() : m_BackgroundImage.rectTransform);
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
            UIParent = parent.gameObject; 
        }

        public void ReturnUIObject(GameObject uiObject)
        {
            ReturnObject(ref UIObject, uiObject);
        }
        public void ReturnUIParent(GameObject uiParent)
        {
            ReturnObject(ref UIParent, uiParent);
        }
        public void ReturnUIDragTarget(GameObject uiDragTarget)
        {
            ReturnObject(ref UIDragTarget, uiDragTarget);
        } 

        public void ReturnObject(ref GameObject go, GameObject inObject)
        {
            
            if(go == null)
            {
                go = inObject;
            }
            else if(go != inObject)
            {
                if (Application.isPlaying)
                    Destroy(inObject);
                else
                    DestroyImmediate(inObject);
            }
        }

        public void OnBeforeAssemblyReload()
        {
            Debug.Log("Before Assembly Reload");
            Debug.Log(UIObject);
        }

        public void OnAfterAssemblyReload()
        {
            Debug.Log("Did I lose references because of assembly reload?");
            Debug.Log(UIObject);
        }

        public Rect CalculateInitialPosition(float padding, in RectTransform parent)
        { 
            Rect r = new Rect();
            Vector2 size = CalculateObjectSize();
            r.position = new Vector2(-size.x * 0.5f, size.y * 0.5f)
                + new Vector2(-padding, padding);
            r.size = size;
            return r;
        }

        public Rect CalculatePosition(float padding, ref Rect latestPosition,
            in RectTransform parent, in Inventory inv, ref float maxWidth)
        {
            Vector2 size = inv.CalculateObjectSize();

            if(parent.rect.height - (latestPosition.y + latestPosition.height * 0.5f) < size.y)
            {
                latestPosition.y = padding + size.y * 0.5f;
                latestPosition.x = latestPosition.x + latestPosition.width * 0.5f
                    - padding - maxWidth - size.x * 0.5f;
                latestPosition.size = size;
                maxWidth = size.x;
            }
            else
            {
                latestPosition.y += padding + latestPosition.height * 0.5f + size.y * 0.5f;
                latestPosition.x += latestPosition.width * 0.5f - size.x * 0.5f;
                maxWidth = Mathf.Max(maxWidth, size.x);
                latestPosition.size = size;
            }
            return latestPosition;
        }

        private Vector2 CalculateInnerSize()
        {
            return new Vector2(Cols, Rows) * SlotWidth
                + new Vector2(Cols, Rows) * Padding;
        }

        private Vector2 CalculateObjectSize()
        {
            return CalculateInnerSize() + Vector2.one * Padding 
                + Vector2.up * (DrawHeader ? Headerheight : 0f);
        }

        public void OnBeforeSerialize()
        {
            //if (UIObject)
            //{
            //    UIObject.AddComponent<InventorySceneReference>().Set(
            //        this, new SerializedObject(this).FindProperty(nameof(UIObject)));
            //}
        }

        public void OnAfterDeserialize()
        {
            //EditorApplication.update += RemoveReferences;
        } 

        private void RemoveReferences()
        {
            //Debug.Log("Remove");
            //EditorApplication.update -= RemoveReferences;
        }
#endregion


        //Serialize content?

        //Resize bag?
    }


}