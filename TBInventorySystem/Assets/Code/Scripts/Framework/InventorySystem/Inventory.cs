using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor.Events;
#endif
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using InventorySystem.Framework;

namespace InventorySystem
{

    [System.Serializable]
    public struct InventoryItem
    {
        public ItemData Data;
        public int Amount;

        public InventoryItem(ItemData itemData, int amount)
        {
            Data = itemData;
            Amount = amount;
        }

        /// <summary>
        /// Adds amount of items to the stack. Returning any overflow of items.
        /// </summary>
        /// <param name="inAmount">Amount to be added.</param>
        /// <returns>Overflow of items.</returns>
        public int AddAmount(int inAmount)
        {
            int overflow = Mathf.Clamp(Data.StackSize - (Amount + inAmount), int.MinValue, 0);
            Amount += inAmount - overflow;
            return overflow;
        }

        public InventoryItem TakeAmount(int amount)
        {
            return new InventoryItem(Data, Mathf.Min(Amount, amount));
        }

        public void RemoveAmount(int inAmount)
        {
            Amount -= inAmount;
            Amount = Mathf.Clamp(Amount, 0, int.MaxValue);
        }

        public int TakeAll(out ItemData item)
        {
            item = Data;
            int returnAmount = Amount;

            Data = null;
            Amount = 0;
            return returnAmount;
        }
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
        public GameObject UIDragTarget = null;

        public GameObject UIParent = null;
        public GameObject UIObject = null;
        public InventoryData m_InventoryData;

        public InventoryItem[] Items;
        public SlotInteraction[] Slots;

        public UnityEvent<Inventory, InventoryItem> OnShiftClickItemCallback = new UnityEvent<Inventory, InventoryItem>();
        public UnityEvent<Inventory, InventoryItem> OnCtrlClickItemCallback = new UnityEvent<Inventory, InventoryItem>();

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

            if(Items == null || (Items != null && Items.Length == 0))
            {
                Items = new InventoryItem[Rows * Cols];
            }

            if(Application.isPlaying)
            {
                UpdateUISprites();
            }
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            s_InventoryInstances.Remove(this);
        }
#endif

        // Called on scene load to imitate an "Awake" function. Handles toggle input.
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
#endregion // ToggleInput

        // Handles generating and destroying of UI
#region InventoryUI
        public void RegenerateUI(in RectTransform parent = null)
        {
            DestroyUI();
            GenerateUI(parent, true);   
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

        public void GenerateUI(in RectTransform inParentGroup = null, bool regenerating = false)
        {
            Transform parent = inParentGroup ? inParentGroup
                : UIParent ? UIParent.GetComponent<RectTransform>()
                : m_InventoryData.InventoryCanvas.transform;

            GameObject background = m_InventoryData.CreateBackground(parent);

            // Keep references to scene objects alive in Editor and Builds.
#region Scene Object Reference handling
#if UNITY_EDITOR
            // UIObject
            InventorySceneReference sceneRef = background.AddComponent<InventorySceneReference>();
            UnityEventTools.AddPersistentListener(sceneRef.ReturnObject, ReturnUIObject);
            sceneRef.ReturnObject.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);

            // UIParent
            if (!regenerating)
            {
                sceneRef = parent.gameObject.AddComponent<InventorySceneReference>();
                UnityEventTools.AddPersistentListener(sceneRef.ReturnObject, ReturnUIParent);
                sceneRef.ReturnObject.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            }
            if (inParentGroup)
            {
                // Drag target (Group Parent)
                sceneRef = inParentGroup.gameObject.AddComponent<InventorySceneReference>(); 
                UnityEventTools.AddPersistentListener(sceneRef.ReturnObject, ReturnUIDragTarget); 
                sceneRef.ReturnObject.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            }
            else
            {
                // Drag target (Self)
                sceneRef = background.AddComponent<InventorySceneReference>();
                UnityEventTools.AddPersistentListener(sceneRef.ReturnObject, ReturnUIDragTarget);
                sceneRef.ReturnObject.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            }
#endif
#endregion // Reference Handling

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

                    slotPosition.x = x * SlotWidth + x * Padding;
                    slotPosition.y = -(y * SlotWidth + y * Padding);

                    slotPosition += new Vector2(-innerSize.x, innerSize.y) * 0.5f;
                    slotPosition += new Vector2(SlotWidth, -SlotWidth) * 0.5f;

                   if (DrawHeader)
                        slotPosition -= Vector2.up * Headerheight * 0.5f;

                    slot.GetComponent<RectTransform>().anchoredPosition = slotPosition;

                    SlotInteraction interaction = slot.GetComponentInChildren<SlotInteraction>();
                    interaction.BindProperties(this);
                    if(Items[y * Cols + x].Data != null)
                    {
                        interaction.SetSprite(Items[y * Cols + x]);
                    }
                }
            }

            UIObject = background;
            UIParent = parent.gameObject;

#if UNITY_EDITOR
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif
        }

        public void ReturnUIObject(InventorySceneReference sceneRef, GameObject uiObject)
        {
            ReturnObject(ref UIObject, uiObject);
        }
        public void ReturnUIParent(InventorySceneReference sceneRef, GameObject uiParent)
        {
            if(UIParent == null)
            {
                UIParent = uiParent;
            }
            else if(UIParent != uiParent)
            {
                Debug.Log("Destroying UIParent");
                if (Application.isPlaying)
                    Destroy(sceneRef);
                else
                    DestroyImmediate(sceneRef);
            }
        }
        public void ReturnUIDragTarget(InventorySceneReference sceneRef, GameObject uiDragTarget)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="padding"></param>
        /// <param name="latestPosition"></param>
        /// <param name="parent"></param>
        /// <param name="inv"></param>
        /// <param name="maxWidth"></param>
        /// <returns></returns>
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
                + new Vector2(Cols-1, Rows-1) * Padding;
        }

        private Vector2 CalculateObjectSize()
        {
            return CalculateInnerSize() + Vector2.one * Padding * 2f 
                + Vector2.up * (DrawHeader ? Headerheight : 0f);
        }

        /// <summary>
        /// Attaches scene reference callback to new parent.
        /// </summary>
        /// <param name="parent">Parent in scene.</param>
        public void SetUIParent(RectTransform parent)
        {
            UIParent = parent.gameObject;
#if UNITY_EDITOR
            InventorySceneReference sceneRef = parent.gameObject.AddComponent<InventorySceneReference>();
            UnityEventTools.AddPersistentListener(sceneRef.ReturnObject, ReturnUIParent);
            sceneRef.ReturnObject.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
#endif
        }
#endregion // Inventory UI

        public InventoryItem FetchItem(int Row, int Col)
        {
            Assert.IsTrue(Row < Rows && Col < Cols,
                string.Format("Array out of range. Row: {0}, Col: {1}", Row, Col));
            return Items[Row * Cols + Col];
        }

        public InventoryItem[] FetchInventory()
        {
            return Items;
        }

        public bool GetFirstEmptySlot(out int row, out int col)
        {
            for(int i = 0; i < Items.Length; i++)
            {
                if(Items[i].Data == null)
                {
                    col = i % Cols;
                    row = (i - col) / Rows;
                    return true;
                }
            }

            row = -1;
            col = -1;
            return false;
        }

        public InventoryItem[] FetchInventoryGroup(Inventory[] inventories)
        {
            List<InventoryItem> items = new List<InventoryItem>();

            foreach(Inventory inv in inventories)
            {
                if(inv != null)
                {
                    items.AddRange(inv.FetchInventory());
                }
            }

            return items.ToArray();
        }

        public int ItemSlotToIndex(GameObject itemSlot)
        {
            Vector2 size = CalculateInnerSize();


            Vector2 slotTopLeft = itemSlot.transform.parent.parent.GetComponent<RectTransform>().anchoredPosition + new Vector2(-SlotWidth, SlotWidth) * 0.5f;

            Vector2 slotDelta = slotTopLeft + size * 0.5f;

            int colIndex = Mathf.RoundToInt((slotDelta.x / size.x) * Cols);
            int rowIndex = Mathf.RoundToInt((1-(slotDelta.y / size.y)) * Rows);

            return rowIndex * Cols + colIndex;
        }

        public void SortInventory()
        {
            SlotInteraction[] slots = GetArrangedSlotArray();

            MergeStacks(slots);
            UpdateUISprites(slots);
            IComparer<InventoryItem> comparer = new ItemComparerAndCompacter();
            Array.Sort(Items, comparer);
            UpdateUISprites();
        }

        private void MergeStacks(SlotInteraction[] slots = null)
        {
            slots ??= GetArrangedSlotArray();
            for (int i = 0; i < Items.Length; i++)
            {
                if (!Items[i].Data || (Items[i].Amount == Items[i].Data.StackSize))
                    continue;

                for(int j = 0; j < Items.Length; j++)
                {

                    if (!Items[j].Data || Items[i].Data.Category.category != Items[j].Data.Category.category)
                        continue;

                    // Fill left as much as possible
                    int space = Items[i].Data.StackSize - Items[i].Amount;
                    int amountToMove = Mathf.Min(space, Items[j].Amount);
                    Items[i].Amount += amountToMove;
                    Items[j].Amount -= amountToMove;

                    if(Items[j].Amount == 0 && Items[j].Data)
                    {
                        slots[j].RemoveSprite();
                        Items[j].Data = null;
                    }
                }
            }
        }

        public void ShiftClickItem(GameObject itemSlot)
        {
            InventoryItem item = Items[ItemSlotToIndex(itemSlot)];
            OnShiftClickItemCallback.Invoke(this, item);
        }

        public void CtrlClickItem(GameObject itemSlot)
        {
            InventoryItem item = Items[ItemSlotToIndex(itemSlot)];
            OnCtrlClickItemCallback.Invoke(this, item);
        }

        public bool AddItemToSlot(InventoryItem item, int row, int col)
        {
            Assert.IsTrue(row < Rows && col < Cols, 
                string.Format("Row or col index out of range", "Row: {0} ({1}), Col: {2} ({3})", row, Rows, col, Cols));

            InventoryItem slot = Items[row * Cols + col];
            

            if(slot.Data != null)
            {
                return false;
            }

            Items[row * Cols + col] = item;
            return true;
        }

        public void UpdateUISprites(SlotInteraction[] slots = null)
        {
            slots ??= GetArrangedSlotArray();
            
            for(int i = 0; i < slots.Length; i++)
            {
                
                if (Items[i].Data == null)
                {
                    slots[i].RemoveSprite();
                }
                else if (Items[i].Amount == 0)
                {
                    slots[i].RemoveSprite();
                    Items[i].Data = null;
                }
                else
                {
                    slots[i].SetSprite(Items[i]);
                }
            }
        }

        //Serialize content?

        //Resize bag?
        private void OnValidate()
        {
            if(Items.Length != Cols * Rows)
            {
                Array.Resize(ref Items, Cols * Rows);
            }
            //Clamp item amount between 0 and stacksize
            for(int i = 0; i < Items.Length; i++)
            {
                if (Items[i].Data)
                {
                    if (Items[i].Amount > Items[i].Data.StackSize)
                    {
                        Items[i].Amount = Items[i].Data.StackSize;
                    }
                    else if (Items[i].Amount < 0)
                    {
                        Items[i].Amount = 0;
                    }
                }
            }

        }
        private SlotInteraction[] GetArrangedSlotArray()
        {
            SlotInteraction[] slots = UIObject.transform.GetComponentsInChildren<SlotInteraction>();

            Assert.IsTrue(slots.Length == Items.Length, "The item array and amount of slots " +
                "do not match. It is recommended to regenerate the inventory.");

            SlotInteraction[] orderedSlots = new SlotInteraction[Items.Length];
            
            for(int i = 0; i < slots.Length; i++)
            {
                int index = ItemSlotToIndex(slots[i].gameObject);
                orderedSlots[index] = slots[i];
            }

            return orderedSlots;
        }
    }


    public class ItemComparerAndCompacter : IComparer<InventoryItem>
    {

        public int Compare(InventoryItem a, InventoryItem b)
        {
            if (!a.Data && !b.Data)
                return 0;

            // Object takes precedence over null
            if (a.Data == null)
                return 1;
            else if (b.Data == null)
                return -1;

            // Same item type
            if (a.Data.Category.category == b.Data.Category.category)
            {
                // Compare amount
                return a.Amount < b.Amount ? 1 : a.Amount == b.Amount ? 0 : -1;
            }

            int aCat = (int)a.Data.Category.category;
            int bCat = (int)b.Data.Category.category;

            // Lower category takes precedence
            return aCat < bCat ? -1 : aCat == bCat ? 0 : 1;
        }
    }

}