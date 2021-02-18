using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace InventorySystem
{
    [System.Serializable]
    public struct ItemCategory
    {
        public EItemCategory Category;
        public EItemCategory ParentCategory;

        [SerializeField] private string m_RefreshCategory;
        [SerializeField] private string m_RefreshParent;

        public ItemCategory(string category, EItemCategory parentCategory)
        {
            Category = 0;
            ParentCategory = parentCategory;

            m_RefreshCategory = category;
            m_RefreshParent = System.Enum.GetName(typeof(EItemCategory), parentCategory);
        }

        public ItemCategory(string category, string parentCategory)
        {
            Category = 0;
            ParentCategory = 0;
            m_RefreshCategory = category;
            m_RefreshParent = parentCategory;
        }

        public string GetName()
        {
            return m_RefreshCategory;
        }

        public string GetParentName()
        {
            return m_RefreshParent;
        }

        public bool IsRoot()
        {
            return Category == ParentCategory;
        }

        public int Level(ItemManager itemManager)
        {
            EItemCategory currentCat = Category;
            int level = 0;
            while(currentCat != itemManager.Categories[(int)currentCat].ParentCategory)
            {
                currentCat = itemManager.Categories[(int)currentCat].ParentCategory;
                level++;
            }
            return level;
        }

        /// <summary>
        /// The CategoryTool takes some time to reload scripts.
        /// Use this to make sure Enum values aren't overwritten.
        /// </summary>
        public void RefreshEnums()
        {
            Category = (EItemCategory)System.Enum.Parse(typeof(EItemCategory), m_RefreshCategory, false);
            ParentCategory = (EItemCategory)System.Enum.Parse(typeof(EItemCategory), m_RefreshParent, false);
        }
    }


    [CreateAssetMenu(fileName = "ItemManager", menuName = "ScriptableObjects/InventorySystem/ItemManager")]
    public class ItemManager : ScriptableObject
    {
        //System.NonSerialized
        [SerializeField] private ItemCategory[] m_Categories = new ItemCategory[] { };
        [SerializeField] private bool[] m_Foldouts = new bool[] { };
        [SerializeField] private bool[] m_SearchedForFoldouts = new bool[] { };
        [SerializeField] private string[] m_ItemWindowInputs = new string[] { "" };
        
        public ItemCategory[] Categories => m_Categories;
        public bool[] CategoryFoldouts => m_Foldouts;
        public bool[] SearchedForFoldouts => m_SearchedForFoldouts;
        public bool IsFoldedOut(int index, bool searching)
        {
            return searching ? SearchedForFoldouts[index] : CategoryFoldouts[index];
        }

        public string[] ItemWindowInputList => m_ItemWindowInputs;

        private void OnEnable()
        {
#if UNITY_EDITOR
            m_AsSerialized ??= new SerializedObject(this);
#endif // UNITY_EDITOR
        }

        public ref ItemCategory FetchCategoryRef(int index)
        {
            return ref m_Categories[index];
        }

        public ref ItemCategory FetchParentRef(int index)
        {
            return ref FetchCategoryRef((int)FetchCategoryRef(index).ParentCategory);
        }

        public bool FetchFoldout(int index)
        {
            return m_Foldouts[index];
        }
#if UNITY_EDITOR
        [SerializeField] [HideInInspector] private SerializedObject m_AsSerialized;

        public void AddItemCategory(int insertAt, in ItemCategory inCategory)
        {
            if (insertAt == m_Categories.Length)
            {
                m_AsSerialized.FindProperty(nameof(m_Categories)).arraySize += 1;
                m_AsSerialized.FindProperty(nameof(m_Foldouts)).arraySize += 1;
                m_AsSerialized.FindProperty(nameof(m_SearchedForFoldouts)).arraySize += 1;
                m_AsSerialized.FindProperty(nameof(m_ItemWindowInputs)).arraySize += 1;
            }
            else
            {
                m_AsSerialized.FindProperty(nameof(m_Categories)).InsertArrayElementAtIndex(insertAt);
                m_AsSerialized.FindProperty(nameof(m_Foldouts)).InsertArrayElementAtIndex(insertAt);
                m_AsSerialized.FindProperty(nameof(m_SearchedForFoldouts)).InsertArrayElementAtIndex(insertAt);
                m_AsSerialized.FindProperty(nameof(m_ItemWindowInputs)).InsertArrayElementAtIndex(insertAt+1);
            }
            m_AsSerialized.ApplyModifiedProperties();
            m_Categories[insertAt] = inCategory;
            m_Foldouts[insertAt] = false;
            m_SearchedForFoldouts[insertAt] = false;
            m_ItemWindowInputs[insertAt] = string.Empty;
        }

        public void RemoveItemCategory(int removeAt)
        {
            m_AsSerialized.FindProperty(nameof(m_Categories)).DeleteArrayElementAtIndex(removeAt);
            m_AsSerialized.FindProperty(nameof(m_Foldouts)).DeleteArrayElementAtIndex(removeAt);
            m_AsSerialized.FindProperty(nameof(m_SearchedForFoldouts)).DeleteArrayElementAtIndex(removeAt);
            m_AsSerialized.FindProperty(nameof(m_ItemWindowInputs)).DeleteArrayElementAtIndex(removeAt+1);
            m_AsSerialized.ApplyModifiedProperties();
        }
#endif // UNITY_EDITOR
    }
}
