using UnityEditor;
using UnityEngine;

namespace CategorySystem.Editor
{
    public class CategorySystemWindow : EditorWindow
    {
        #region LayoutProperties
        // Window
        private Vector2 m_WindowScrollPosition = Vector2.zero;

        // Settings
        private bool m_ShowSettings = false;
        #endregion //LayoutProperties

        #region InteractiveProperties
        private string m_SearchForItems = string.Empty;
        [SerializeField] [HideInInspector] private bool m_BufferRefreshEnum = false;
        [SerializeField] [HideInInspector] private bool m_RefreshManually = false;
        [SerializeField] [HideInInspector] private bool m_ForceBuffer = false;
        #endregion


        [SerializeField] private SerializedObject m_AsSerialized = null;
        private SerializedObject AsSerialized => m_AsSerialized ??= new SerializedObject(this);

        [SerializeField] private CategoryAPI m_ItemManager = null;


        [MenuItem("Window/ItemManager")]
        public static void ShowWindow()
        {
            GetWindow(typeof(CategorySystemWindow));
        }

        private void OnEnable()
        {
            var editorPref = EditorPrefs.GetString(nameof(m_ItemManager), JsonUtility.ToJson(this, false));
            JsonUtility.FromJsonOverwrite(editorPref, this);

            if(m_ForceBuffer || (m_BufferRefreshEnum && !m_RefreshManually))
            {
                RefreshCategoryEnums();
            }
        }

        private void OnDisable()
        {
            var editorPref = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString(nameof(m_ItemManager), editorPref);
        }

        private void OnGUI()
        {
            #region GUIWindow
            m_WindowScrollPosition = GUILayout.BeginScrollView(m_WindowScrollPosition, GUILayout.ExpandWidth(true));

            #region Settings
            if (m_ShowSettings = EditorGUILayout.Foldout(m_ShowSettings, "Settings", true))
            {
                EditorGUILayout.PropertyField(AsSerialized.FindProperty(nameof(m_ItemManager)));
                EditorGUILayout.PropertyField(AsSerialized.FindProperty(nameof(m_RefreshManually)));
                AsSerialized.ApplyModifiedProperties();
            }

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.ExpandWidth(true));

            if(!m_ItemManager)
            {
                GUILayout.Label("Set ItemManager in Settings.");
                GUILayout.EndScrollView();
                return;
            }
            #endregion // Settings

            GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
            GUILayout.Label("Items");

            string searchInput = CustomGUIUtility.TextFieldWithPlaceHolder("Search for items\t\t", 
                "SearchForItemsID", m_SearchForItems, true);

            m_SearchForItems = searchInput.Replace(" ", ""); ;

            if(m_RefreshManually && GUILayout.Button("Refresh"))
            {
                m_ForceBuffer = true;
                AssetDatabase.Refresh();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider, GUILayout.ExpandWidth(true));

            if(m_BufferRefreshEnum)
            {
                EditorGUILayout.LabelField("Refreshing scripts" + ".".Repeat((int)(Time.time % 3 + 1)));
                GUILayout.EndScrollView();
                return;
            }

            int level = 0;
            GUIAddCategoryInput(level, 0, 0);

            bool searching = m_SearchForItems.Length > 0;

            for (int i = 0; i < m_ItemManager.Categories.Length; i++)
            {
                if (!m_ItemManager.FetchCategoryRef(i).IsRoot() 
                    && !AllParentsFoldedOut(i, searching))//!m_ItemManager.FetchFoldout((int)m_ItemManager.FetchCategoryRef(i).ParentCategory))
                {
                    continue;
                }

                if (searching && !HasMatchingChild(i, m_SearchForItems))
                {
                    m_ItemManager.SearchedForFoldouts[i] = false;
                    continue;
                }
                else if(searching)
                {
                    m_ItemManager.SearchedForFoldouts[i] = true;
                }

                GUILayout.BeginHorizontal();


                level = m_ItemManager.FetchCategoryRef(i).Level(m_ItemManager);

                GUILayout.Label("\t".Repeat(level), GUILayout.ExpandWidth(false));

                if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Minus"), GUILayout.ExpandWidth(false)))
                {
                    RecursiveDeletion(i);
                    if (!m_RefreshManually)
                    {
                        AssetDatabase.Refresh();
                        m_BufferRefreshEnum = true;
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndScrollView();
                    return;
                }

                bool foldedOut = EditorGUILayout.Foldout(m_ItemManager.IsFoldedOut(i, searching)
                    , m_ItemManager.FetchCategoryRef(i).Category.ToCategoryString());

                if (!searching)
                    m_ItemManager.CategoryFoldouts[i] = foldedOut;


                

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if ((m_ItemManager.IsFoldedOut(i, searching)))
                {
                    GUIAddCategoryInput(level+1, i+1, (CategoryName)i);
                }
            }

            GUILayout.EndScrollView();
            #endregion //GUIWindow
        }

        private void GUIAddCategoryInput(int level, int enumIndex, CategoryName parent)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("\t".Repeat(level), GUILayout.ExpandWidth(false));

            string currentInput = CustomGUIUtility.TextFieldWithPlaceHolder("Add category\t\t",
                "AddMainCategoryID" + enumIndex.ToString(), m_ItemManager.ItemWindowInputList[enumIndex], false);

            m_ItemManager.ItemWindowInputList[enumIndex] = EnumifyInput(currentInput);

            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"), GUILayout.ExpandWidth(false)))
            {
                if (!string.IsNullOrEmpty(m_ItemManager.ItemWindowInputList[enumIndex].Trim()))
                {
                    name = string.Empty;
                    if(level == 0)
                    {
                        name = m_ItemManager.ItemWindowInputList[enumIndex];
                    }
                    else
                    {
                        name = System.Enum.GetName(typeof(CategoryName), parent) 
                            + m_ItemManager.ItemWindowInputList[enumIndex];
                    }

                    CategorySystemUtility.AddCategoryEnumToFile(
                        name
                        , enumIndex
                        , m_ItemManager);


                    if (!m_RefreshManually)
                    {
                        AssetDatabase.Refresh();
                        m_BufferRefreshEnum = true;
                    }

                    if (level == 0)
                    {
                        m_ItemManager.AddItemCategory(enumIndex
                           , new CategoryData(name
                           , name));
                    }
                    else
                    {
                        m_ItemManager.AddItemCategory(enumIndex
                           , new CategoryData(name
                           , parent));
                    }
                    
                }
            }
            GUILayout.EndHorizontal();
        }

        
        private void RefreshCategoryEnums()
        {
            for (int i = 0; i < m_ItemManager.Categories.Length; i++)
            {
                m_ItemManager.FetchCategoryRef(i).RefreshEnums();
            }
            m_BufferRefreshEnum = false;
            m_ForceBuffer = false;
        }

        private bool HasMatchingChild(int index, string match)
        {
            int level = m_ItemManager.FetchCategoryRef(index).Level(m_ItemManager);

            do
            {
                if (m_ItemManager.FetchCategoryRef(index).GetName().ToLower()
                    .Contains(value: match.ToLower()))
                {
                    return true;
                }
                index++;

                if(index >= m_ItemManager.Categories.Length)
                {
                    break;
                }
            }
            while (level < m_ItemManager.FetchCategoryRef(index).Level(m_ItemManager));


            return false;
        }

        private string EnumifyInput(string input)
        {
            input = input.Replace(" ", "");

            if (input.Length > 0)
            {
                input = input.ToLower();
                input = input.ToString().ToUpper()[0] + input.Substring(1, input.Length - 1);
            }
            return input;
        }

        private void RecursiveDeletion(int index)
        {
            int level = m_ItemManager.FetchCategoryRef(index).Level(m_ItemManager);

            int amountToDelete = 0;

            int it = index;

            do
            {
                amountToDelete++;
            }
            while (++it < m_ItemManager.Categories.Length
            && level < m_ItemManager.FetchCategoryRef(it).Level(m_ItemManager));

            for(int i = 0; i < amountToDelete; i++)
            {
                CategorySystemUtility.RemoveCategoryEnumFromFile(index, m_ItemManager);
                m_ItemManager.RemoveItemCategory(index);
            }
        }

        private bool AllParentsFoldedOut(int index, bool searching)
        {
            CategoryName currentCategory = m_ItemManager.FetchCategoryRef(index).Category;
            CategoryName currentParent = m_ItemManager.FetchCategoryRef(index).ParentCategory;

            while(currentParent != currentCategory)
            {
                if(!m_ItemManager.IsFoldedOut((int)currentParent, searching))
                {
                    return false;
                }
                currentCategory = currentParent;
                currentParent = m_ItemManager.FetchCategoryRef((int)currentCategory).ParentCategory;
            }
            return true;
        }
    }
}