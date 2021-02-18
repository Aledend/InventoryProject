using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace CategorySystem
{

    [System.Serializable]
    public struct CategoryData
    {
        public CategoryName Category;
        [HideInInspector] public CategoryName ParentCategory;

        [SerializeField] [HideInInspector] private string m_RefreshCategory;
        [SerializeField] [HideInInspector] private string m_RefreshParent;

        public CategoryData(string category, CategoryName parentCategory)
        {
            Category = 0;
            ParentCategory = parentCategory;

            m_RefreshCategory = category;
            m_RefreshParent = System.Enum.GetName(typeof(CategoryName), parentCategory);
        }

        public CategoryData(string category, string parentCategory)
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

        public int Level(CategoryAPI categoryAPI)
        {
            CategoryName currentCat = Category;
            int level = 0;
            while (currentCat != categoryAPI.Categories[(int)currentCat].ParentCategory)
            {
                currentCat = categoryAPI.Categories[(int)currentCat].ParentCategory;
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
            Category = (CategoryName)System.Enum.Parse(typeof(CategoryName), m_RefreshCategory, false);
            ParentCategory = (CategoryName)System.Enum.Parse(typeof(CategoryName), m_RefreshParent, false);
        }
    }


    [CreateAssetMenu(fileName = "CategoryData", menuName = "ScriptableObjects/CategorySystem/CategoryData")]
    public class CategoryAPI : ScriptableObject
    {
        [SerializeField] private CategoryData[] m_Categories = new CategoryData[] { };
        public CategoryData[] Categories => m_Categories;

        #region Editor
#if UNITY_EDITOR
        [SerializeField] private bool[] m_Foldouts = new bool[] { };
        [SerializeField] private bool[] m_SearchedForFoldouts = new bool[] { };
        [SerializeField] private string[] m_EditorWindowInputs = new string[] { "" };
        public bool[] CategoryFoldouts => m_Foldouts;
        public bool[] SearchedForFoldouts => m_SearchedForFoldouts;
        public string[] ItemWindowInputList => m_EditorWindowInputs;
        public bool IsFoldedOut(int index, bool searching)
        {
            return searching ? SearchedForFoldouts[index] : CategoryFoldouts[index];
        }
#endif // UNITY_EDITOR
        #endregion // Editor

        private void OnEnable()
        {
            #region Editor
#if UNITY_EDITOR
            m_AsSerialized ??= new SerializedObject(this);
#endif // UNITY_EDITOR
            #endregion // Editor
        }

        public ref CategoryData FetchCategoryRef(int index)
        {
            return ref m_Categories[index];
        }


        #region Editor
#if UNITY_EDITOR
        public bool FetchFoldout(int index)
        {
            return m_Foldouts[index];
        }
        [SerializeField] [HideInInspector] private SerializedObject m_AsSerialized;

        public void AddItemCategory(int insertAt, in CategoryData inCategory)
        {
            if (insertAt == m_Categories.Length)
            {
                m_AsSerialized.FindProperty(nameof(m_Categories)).arraySize += 1;
                m_AsSerialized.FindProperty(nameof(m_Foldouts)).arraySize += 1;
                m_AsSerialized.FindProperty(nameof(m_SearchedForFoldouts)).arraySize += 1;
                m_AsSerialized.FindProperty(nameof(m_EditorWindowInputs)).arraySize += 1;
            }
            else
            {
                m_AsSerialized.FindProperty(nameof(m_Categories)).InsertArrayElementAtIndex(insertAt);
                m_AsSerialized.FindProperty(nameof(m_Foldouts)).InsertArrayElementAtIndex(insertAt);
                m_AsSerialized.FindProperty(nameof(m_SearchedForFoldouts)).InsertArrayElementAtIndex(insertAt);
                m_AsSerialized.FindProperty(nameof(m_EditorWindowInputs)).InsertArrayElementAtIndex(insertAt + 1);
            }
            m_AsSerialized.ApplyModifiedProperties();
            m_Categories[insertAt] = inCategory;
            m_Foldouts[insertAt] = false;
            m_SearchedForFoldouts[insertAt] = false;
            m_EditorWindowInputs[insertAt] = string.Empty;
        }

        public void RemoveItemCategory(int removeAt)
        {
            m_AsSerialized.FindProperty(nameof(m_Categories)).DeleteArrayElementAtIndex(removeAt);
            m_AsSerialized.FindProperty(nameof(m_Foldouts)).DeleteArrayElementAtIndex(removeAt);
            m_AsSerialized.FindProperty(nameof(m_SearchedForFoldouts)).DeleteArrayElementAtIndex(removeAt);
            m_AsSerialized.FindProperty(nameof(m_EditorWindowInputs)).DeleteArrayElementAtIndex(removeAt + 1);
            m_AsSerialized.ApplyModifiedProperties();
        }
#endif // UNITY_EDITOR
        #endregion // Editor
    }

}