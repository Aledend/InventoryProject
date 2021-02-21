using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using InventorySystem.Framework;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor.Events;
#endif

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
        [HideInInspector] public SlotInteraction[] Slots;

        #region UnityEvent's
        [HideInInspector] public UnityEvent<Inventory, InventoryItem, int> OnMouseDownInventorySlot = 
            new UnityEvent<Inventory, InventoryItem, int>();
        [HideInInspector] public UnityEvent<Inventory, InventoryItem, int> OnMouseUpInventorySlot = 
            new UnityEvent<Inventory, InventoryItem, int>();
        [HideInInspector] public UnityEvent<Inventory, InventoryItem, int> OnMouseEnterInventorySlot =
            new UnityEvent<Inventory, InventoryItem, int>();
        [HideInInspector] public UnityEvent<Inventory, InventoryItem, int> OnMouseExitInventorySlot =
            new UnityEvent<Inventory, InventoryItem, int>();
        #endregion

        // Handles showing and hiding of UI
        #region InputHandling
        // Toggling function applied to the given input listener in OnBeforeSceneLoadRuntimeMethod
        private void ToggleActive()
        {
            if(UIObject)
            {
                if (UIObject.activeSelf)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                    UIObject.SetActive(false);
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    UIObject.SetActive(true);
                }
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

            if(UIObject && Application.isPlaying)
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
        public void RegenerateUI(RectTransform parent = null, bool keepSizeAndPosition = true)
        {
            RectTransformData rectData = null;

            if(UIObject)
            {
                rectData = new RectTransformData(UIObject.GetComponent<RectTransform>());
            }

            if (!parent && UIDragTarget)
                parent = UIDragTarget.GetComponent<RectTransform>();

            DestroyUI();
            GenerateUI(parent, true, keepSizeAndPosition, rectData);   
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
                    inv.RegenerateUI(groupParent, false);
                    RectTransform invTrans = inv.UIObject.GetComponent<RectTransform>();
                    invTrans.anchorMax = new Vector2(1f, 0f);
                    invTrans.anchorMin = new Vector2(1f, 0f);
                }
            });


            float inventoryPadding = 15f;
            Rect latestPosition = CalculateInitialPosition(inventoryPadding);
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


        private void PositionUIElement(ref Rect latestPosition)
        {
            UIObject.GetComponent<RectTransform>().anchoredPosition = latestPosition.position;
        }

        public void GenerateUI(in RectTransform inParentGroup = null, bool regenerating = false, 
            bool keepSizeAndPosition = false, RectTransformData rectData = null)
        {
            OnValidate();

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
                SetUIParent(parent.GetComponent<RectTransform>());
            }
            if (inParentGroup)
            {
                Debug.Log("setting parent");
                // Drag target (Group Parent)
                SetUIDragTarget(inParentGroup.GetComponent<RectTransform>());
            }
            else
            {
                Debug.Log("setting self");
                // Drag target (Self)
                SetUIDragTarget(background.GetComponent<RectTransform>());
            }
            #endif
            #endregion // Reference Handling

            Vector2 innerSize = CalculateInnerSize();
            Vector2 backgroundSize = CalculateObjectSize();

            if (keepSizeAndPosition && rectData != null)
                rectData.WriteToRect(background.GetComponent<RectTransform>());

            Image m_BackgroundImage = background.GetComponent<Image>();
            m_BackgroundImage.rectTransform.sizeDelta = backgroundSize;


            #region Generate Header
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
            #endregion // Generate Header

            #region Generate Slots UI
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
            #endregion // Generate Slots UI

            UIObject = background;
            UIParent = parent.gameObject;

#if UNITY_EDITOR
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
#endif
        }

        public void ReturnUIObject(InventorySceneReference sceneRef, GameObject uiObject)
        {
            //Debug.Log("destroyign uiobject " + uiObject + "  and  " + UIObject + sceneRef.ReturnObject);
            if (uiObject == null)
            {
                if (Application.isPlaying)
                    Destroy(sceneRef);
                else
                    DestroyImmediate(sceneRef);
            }
            else if (UIObject == null)
            {
                UIObject = uiObject;
            }
            else if (UIObject != uiObject)
            {
                Debug.Log("destroying uiobject");
                if (Application.isPlaying)
                    Destroy(uiObject);
                else
                {
                    DestroyImmediate(uiObject);
                }
            }
            //ReturnObject(ref UIObject, uiObject);
        }
        public void ReturnUIParent(InventorySceneReference sceneRef, GameObject uiParent)
        {
            if (UIParent == null)
            {
                UIParent = uiParent;
            }
            else if (UIParent != uiParent)
            {
                Debug.Log("destroying parent");
                if (Application.isPlaying)
                    Destroy(uiParent);
                else
                {
                    DestroyImmediate(uiParent);
                }
            }
            //if(UIParent == null)
            //{
            //    UIParent = uiParent;
            //}
            //else if(UIParent != uiParent)
            //{
            //    Debug.Log("destroyign sceneref");
            //    if (Application.isPlaying)
            //        Destroy(sceneRef);
            //    else
            //        DestroyImmediate(sceneRef);
            //}
        }
        public void ReturnUIDragTarget(InventorySceneReference sceneRef, GameObject uiDragTarget)
        {
            if (UIDragTarget == null)
            {
                UIDragTarget = uiDragTarget;
            }
            else if (UIDragTarget != uiDragTarget)
            {
                Debug.Log("destroying dragtarget");
                if (Application.isPlaying)
                    Destroy(uiDragTarget);
                else
                {
                    DestroyImmediate(uiDragTarget);
                }
            }
        } 


        public Rect CalculateInitialPosition(float padding)
        { 
            Rect r = new Rect();
            Vector2 size = CalculateObjectSize();
            r.position = new Vector2(-size.x * 0.5f, size.y * 0.5f)
                + new Vector2(-padding, padding);
            r.size = size;
            return r;
        }

        /// <summary>
        /// Positions Multiple inventories after each other, Starting from
        /// the bottom right corner and wraps around when reaching the upper
        /// screen bounds.
        /// </summary>
        /// <param name="padding">Distance between inventories</param>
        /// <param name="latestPosition">Rect of last inventorie that was drawn.</param>
        /// <param name="parent">Group Parent</param>
        /// <param name="inv">Which inventory to adjust size after.</param>
        /// <param name="maxWidth">Widest inventory so far.</param>
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
            foreach(InventorySceneReference reference in parent.GetComponents<InventorySceneReference>())
            {
                Debug.Log(reference == this);
                if(reference.InventoryReference == this)
                {
                    return;
                }
            }

            InventorySceneReference sceneRef = parent.gameObject.AddComponent<InventorySceneReference>();
            sceneRef.InventoryReference = this;
            UnityEventTools.AddPersistentListener(sceneRef.ReturnObject, ReturnUIParent);
            sceneRef.ReturnObject.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
#endif
        }

        private void SetUIDragTarget(RectTransform target)
        {
            UIDragTarget = target.gameObject;
#if UNITY_EDITOR
            foreach (InventorySceneReference reference in target.GetComponents<InventorySceneReference>())
            {
                if (reference.InventoryReference == this)
                {
                    return;
                }
            }

            InventorySceneReference sceneRef = target.gameObject.AddComponent<InventorySceneReference>();
            sceneRef.InventoryReference = this;
            UnityEventTools.AddPersistentListener(sceneRef.ReturnObject, ReturnUIDragTarget);
            sceneRef.ReturnObject.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
#endif // UNITY_EDITOR
        }

        #endregion // Inventory UI

        /// <summary>
        /// Receives inventory item without decrementing amount.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public InventoryItem FetchItem(int Row, int Col)
        {
            return FetchItem(Row * Cols + Col);
        }

        /// <summary>
        /// Receives inventory item without decrementing amount.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public InventoryItem FetchItem(int index)
        {
            return Items[index];
        }

        public InventoryItem[] FetchInventory()
        {
            return Items;
        }

        /// <summary>
        /// Takes inventoryitem and decrements amount.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>If there is a valid item on the given index</returns>
        public bool TakeItem(int index, out InventoryItem item)
        {
            if (Items[index].Data && Items[index].Amount > 0)
            {
                Items[index].RemoveAmount(1);
                item = Items[index];
                UpdateUISprites();
                return true;
            }

            item = new InventoryItem();
            return false;
        }

        public bool TakeAll(int index, out InventoryItem item)
        {
            InventoryItem invItem = Items[index];
            if(invItem.Data && invItem.Amount > 0)
            {
                Items[index].Data = null;
                Items[index].Amount = 0;
                item = invItem;
                UpdateUISprites();
                return true;
            }
            item = new InventoryItem();
            return false;
        }

        public bool GetFirstEmptySlot(out int row, out int col)
        {
            if(GetFirstEmptySlot(out int index))
            {
                col = index % Cols;
                row = (index - col) / Rows;
                return true;
            }
            row = -1;
            col = -1;
            return false;
        }

        public bool GetFirstEmptySlot(out int index)
        {
            for (int i = 0; i < Items.Length; i++)
            {
                if (Items[i].Data == null)
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }

        public void SwapItems(Inventory otherInventory, int otherIndex, int thisIndex)
        {
            
            otherInventory.TakeAll(otherIndex, out InventoryItem tempOther);
            TakeAll(thisIndex, out InventoryItem tempThis);

            otherInventory.AddItemToSlot(tempThis, otherIndex);
            AddItemToSlot(tempOther, thisIndex);
        }

        /// <summary>
        /// Calculate the item slots position relative to its root's parent.
        /// Warning: Changing the hierarchy of the itemslot prefab may cause
        /// issues when calculating slotTopLeft
        /// </summary>
        /// <param name="itemSlot">The itemslot UI object</param>
        /// <returns>Itemslot index</returns>
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

        /// <summary>
        /// Merge items of the same type together.
        /// </summary>
        /// <param name="slots"></param>
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

        public void OnMouseDownSlot(GameObject itemSlot)
        {
            int index = ItemSlotToIndex(itemSlot);
            InventoryItem item = Items[index];
            OnMouseDownInventorySlot.Invoke(this, item, index);
        }

        public void OnMouseUpSlot(GameObject itemSlot)
        {
            int index = ItemSlotToIndex(itemSlot);
            InventoryItem item = Items[index];
            OnMouseUpInventorySlot.Invoke(this, item, index);
        }

        public void OnMouseEnterSlot(GameObject itemSlot)
        {
            int index = ItemSlotToIndex(itemSlot);
            InventoryItem item = Items[index];
            OnMouseEnterInventorySlot.Invoke(this, item, index);
        }

        public void OnMouseExitSlot(GameObject itemSlot)
        {
            int index = ItemSlotToIndex(itemSlot);
            InventoryItem item = Items[index];
            OnMouseExitInventorySlot.Invoke(this, item, index);
        }

        public bool AddItemToSlot(InventoryItem item, int row, int col)
        {
            return AddItemToSlot(item, row * Cols + col);
        }

        public bool AddItemToSlot(InventoryItem item, int index)
        {
            Assert.IsTrue(index < Items.Length, "Index out of range when adding item to slot");

            if (Items[index].Data != null)
            {
                return false;
            }
            Items[index] = item;

            UpdateUISprites();

            return true;
        }

        public void ForceAddItem(InventoryItem item, int index)
        {
            Assert.IsTrue(index < Items.Length, "Index out of range when adding item to slot");

            Items[index] = item;

            UpdateUISprites();
        }

        public void UpdateUISprites(SlotInteraction[] slots = null)
        {
            slots ??= GetArrangedSlotArray();

            for (int i = 0; i < slots.Length; i++)
            {
                if (Items[i].Data == null)
                {
                    slots[i].RemoveSprite();
                }
                else if (Application.isPlaying && Items[i].Amount == 0)
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

        public void RemoveItem(int index, SlotInteraction[] slots = null)
        {
            slots ??= GetArrangedSlotArray();
            Items[index].Data = null;
            Items[index].Amount = 0;
            slots[index].RemoveSprite();
            UpdateUISprites(slots);
        }

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

            if(UIObject)
            {
                UpdateUISprites();
            }
        }
        private SlotInteraction[] GetArrangedSlotArray()
        {
            SlotInteraction[] slots = UIObject.transform.GetComponentsInChildren<SlotInteraction>();

            Assert.IsTrue(slots.Length == Items.Length, "The item array and amount of slots " +
                "do not match.\n It is recommended to regenerate the inventory. (Slots: " + slots.Length 
                + ", Items: " + Items.Length + ", " + this + ")");

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