using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace InventorySystem
{
    [CustomPropertyDrawer(typeof(Inventory))]
    public class InventoryDrawer : PropertyDrawer
    {

        private const float k_Spacing = 5f;
        private float m_DropDownAngle = -90f;
        private float m_OptionsAngle = -90f;
        private const float k_DropdownWidth = 15f;
        private const float k_ButtonOffset = 330f;
        private bool m_ShowOptions = false;
        private bool m_HasHeader = false;
        private bool m_Expanded = false;
        private const float k_HorizontalRuleHeight = 10f;
        private const string k_IndentAsString = "     ";
        private RectTransform m_GroupParent = null;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect newPos = position;

            Inventory inventory = property.objectReferenceValue as Inventory;

            // Render Grouping Button if this is the first element of an array
            #region Render Grouping Button
            if (PropertyIsFirstInArray(property))
            {
                newPos.y += FetchButtonSize("").y + k_Spacing * 2f;

                string arrayName = GetPropertyArrayName(property);
                UnityEngine.Object target = property.serializedObject.targetObject;

                System.Type type = target.GetType();
                FieldInfo field = type.GetField(arrayName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

                System.Object value = field.GetValue(target);
                Inventory[] arr = value as Inventory[];

                Inventory first = Array.Find(arr, inv => inv != null);

                for(int i = arr.Length-1; i >= 0; i--)
                {
                    if(ArrayUtility.IndexOf(arr, arr[i]) != ArrayUtility.LastIndexOf(arr, arr[i]))
                    {
                        if (arr[i] && property.serializedObject.targetObject)
                        {
                            Debug.LogWarning("Duplicates found in array. " +
                                "Removed " + arr[i].name + " from " 
                                + property.serializedObject.targetObject.name);
                            arr[i] = null;
                        }
                    }
                }
                #region Render UIGroupButton
                Rect genButtonRect = newPos;
                genButtonRect.x = FetchLabelSize(k_IndentAsString + arrayName).x;
                genButtonRect.y -= FetchLabelSize("Generate UIGroup").y * 1.5f;
                genButtonRect.size = FetchButtonSize("Generate UIGroup");
                #endregion
                #region Render UIGroupLabel
                Rect groupLabel = genButtonRect;
                groupLabel.x += groupLabel.size.x + FetchLabelSize(k_IndentAsString).x;
                groupLabel.size = FetchLabelSize("Group Target:");
                groupLabel.width = position.width - genButtonRect.width < groupLabel.width ? 0f : groupLabel.width;
                GUI.Label(groupLabel, "Group Target:");
                #endregion
                #region Render UIGroupObject
                Rect groupParentRect = groupLabel;
                groupParentRect.x += groupParentRect.size.x + FetchLabelSize(k_IndentAsString).x;
                groupParentRect.width = Mathf.Clamp(groupParentRect.x +  300,
                    0f, Mathf.Clamp(position.width - groupParentRect.x, 0, float.MaxValue));
                m_GroupParent =  EditorGUI.ObjectField(groupParentRect, m_GroupParent, typeof(RectTransform), true) as RectTransform;
                #endregion
                GUI.enabled = m_GroupParent && first;
                if(GUI.Button(genButtonRect, "Generate UIGroup") && first)
                {
                    first.GenerateUIGroup(arr, m_GroupParent);

                    Framework.InventorySceneReference[] references = GameObject.FindObjectsOfType<Framework.InventorySceneReference>();
                    for (int i = 0; i < references.Length; i++)
                    {
                        if (references[i])
                            references[i].ReturnObject.Invoke(references[i], references[i].gameObject);
                    }
                }
                GUI.enabled = true;
                genButtonRect.x += genButtonRect.size.x;

            }
            #endregion

            EditorGUI.BeginChangeCheck();

            // If the Object field isn't null. Render dropdown button.
            if (inventory)
            {
                property.isExpanded = m_Expanded = DrawDropDown(ref newPos, m_Expanded, ref m_DropDownAngle);
            }

            

            // If part of array. Render "Element X"
            string propertyLabel = property.displayName;
            string propPath = property.propertyPath;
            if(propPath.Contains("Array.data["))
            {
                propertyLabel = "Element " + propPath[propPath.Length - 2];
                if(!inventory)
                {
                    propertyLabel = k_IndentAsString + propertyLabel;
                }
            }
                
            // Render default Object field.
            EditorGUI.PropertyField(newPos, property, new GUIContent(propertyLabel), true);

            newPos.y += FetchObjectFieldSize("").y; // Move next rect down
            


            // Resetting GUI and adding spacing if Dropdown is inactive or Object is null.
            if(!property.isExpanded || !inventory)
            {
                if (inventory)
                {
                    EditorGUI.indentLevel--;
                }
                EditorGUI.EndChangeCheck();
                newPos.width = position.width + FetchLabelSize(k_IndentAsString).x;
                newPos.x = position.x - FetchLabelSize(k_IndentAsString).x;
                newPos.height = k_HorizontalRuleHeight;
                EditorGUI.LabelField(newPos, "", GUI.skin.horizontalSlider);
                return;
            }


            DrawSpace(ref newPos); // Spacing

            Rect buttonRect = newPos;

            // Attach Rendered UI to another UI Parent
            #region AttachToUIButton
            GameObject selection = Selection.activeGameObject;

            bool shouldDrawAttachButton = ShouldDrawAttachButton(out RectTransform rect, inventory);
            {
                string labelString;
                if (shouldDrawAttachButton)
                {
                    labelString = string.Format("UIComponent selected ({0})", selection.name);
                }
                else
                {
                    GUI.enabled = false;
                    labelString = "No UIElement selected.";
                }

                Vector2 size = FetchLabelSize(labelString);
                buttonRect.width = size.x;
                buttonRect.height = size.y;
                buttonRect.width += EditorGUI.IndentedRect(buttonRect).x - buttonRect.x;
                EditorGUI.LabelField(buttonRect, labelString);


                size = FetchButtonSize("Attach To UI Element");
                buttonRect.width = size.x;
                buttonRect.height = size.y;

                buttonRect.x = k_ButtonOffset;
                if (GUI.Button(buttonRect, "Attach To UI Element"))
                {
                    inventory.SetUIParent(rect);   
                    if (inventory.UIObject)
                    {
                        inventory.UIObject.transform.SetParent(inventory.UIParent.transform);
                    }
                }
                newPos.y += size.y;
                DrawSpace(ref newPos);

                GUI.enabled = true;
            }
            #endregion // AttachToUIButton

            buttonRect = newPos;

            #region Generate/Regenerate Buttons

            if (ShouldDrawGenerateButton(inventory))
            {
                string labelString = string.Format("UI is not generated.");
                Vector2 size = FetchLabelSize(labelString);


                buttonRect.width = size.x;
                buttonRect.height = size.y;

                buttonRect.width += EditorGUI.IndentedRect(buttonRect).x - buttonRect.x;

                EditorGUI.LabelField(buttonRect, labelString);


                size = FetchButtonSize("Generate");
                buttonRect.width = size.x;
                buttonRect.height = size.y;

                buttonRect.x = k_ButtonOffset;
                if (GUI.Button(buttonRect, "Generate"))
                {
                    inventory.GenerateUI();
                }
                newPos.y += size.y;
                DrawSpace(ref newPos);
            }
            else
            {
                string labelString = string.Format("UI is generated ({0})", inventory.UIObject.name);
                Vector2 size = FetchLabelSize(labelString);
                buttonRect.width = size.x;
                buttonRect.height = size.y;
                buttonRect.width += EditorGUI.IndentedRect(buttonRect).x - buttonRect.x;


                EditorGUI.LabelField(buttonRect, labelString);


                size = FetchButtonSize("Regenerate");
                buttonRect.width = size.x;
                buttonRect.height = size.y;

                buttonRect.x = k_ButtonOffset;
                if (GUI.Button(buttonRect, "Regenerate"))
                {
                    Vector2 pos = inventory.UIObject.GetComponent<RectTransform>().anchoredPosition;
                    inventory.RegenerateUI();
                    inventory.UIObject.GetComponent<RectTransform>().anchoredPosition = pos;
                }
                buttonRect.x += buttonRect.width + k_Spacing;

                if(GUI.Button(buttonRect, "Destroy"))
                {
                    inventory.DestroyUI();
                }

                newPos.y += size.y;
                DrawSpace(ref newPos);
            }

            #endregion // Generate/Regenerate Buttons

            #region Options
            m_ShowOptions = DrawDropDown(ref newPos, m_ShowOptions, ref m_OptionsAngle);
            newPos.height = FetchLabelSize("").y;
            GUI.Label(EditorGUI.IndentedRect(newPos), "Options");
            newPos.y += newPos.height;

            if(m_ShowOptions)
            {
                inventory.Cols = Mathf.Clamp(EditorGUI.IntField(newPos, "Columns", inventory.Cols), 0, int.MaxValue);
                newPos.y += FetchLabelSize("").y;
                DrawSpace(ref newPos);
                inventory.Rows = Mathf.Clamp(EditorGUI.IntField(newPos, "Rows", inventory.Rows), 0, int.MaxValue);
                newPos.y += FetchLabelSize("").y;
                DrawSpace(ref newPos);
                inventory.SlotWidth = Mathf.Clamp(EditorGUI.FloatField(newPos, "Slot Width", inventory.SlotWidth), 0f, float.MaxValue);
                newPos.y += FetchLabelSize("").y;
                DrawSpace(ref newPos);
                inventory.Padding = EditorGUI.FloatField(newPos, "Padding", inventory.Padding);
                newPos.y += FetchLabelSize("").y;
                DrawSpace(ref newPos);
                m_HasHeader = inventory.DrawHeader = EditorGUI.Toggle(newPos, "Draw Header", inventory.DrawHeader);
                newPos.y += FetchToggleSize("").y;
                DrawSpace(ref newPos);
                if(inventory.DrawHeader)
                {
                    inventory.Headerheight = EditorGUI.FloatField(newPos, "Header Height", inventory.Headerheight);
                    newPos.y += FetchLabelSize("").y;
                    DrawSpace(ref newPos);
                    RectTransform dragTarget = EditorGUI.ObjectField(newPos, nameof(inventory.UIDragTarget) +
                        (inventory.UIDragTarget ? string.Empty : " (Defaulted to self)"),
                        inventory.UIDragTarget, typeof(RectTransform), true) as RectTransform;
                    inventory.UIDragTarget = dragTarget ? dragTarget.gameObject : null;
                    newPos.y += newPos.height;
                    DrawSpace(ref newPos);
                }
                inventory.HideOnPlay = EditorGUI.Toggle(newPos, "Hide on Play", inventory.HideOnPlay);
                newPos.y += FetchToggleSize("").y;
                DrawSpace(ref newPos);
                inventory.InventoryToggle = (KeyCode)EditorGUI.EnumPopup(newPos, "ToggleKey", inventory.InventoryToggle);
                newPos.y += FetchEnumFieldSize("").y;
            }
            #endregion // Options

            EditorGUI.indentLevel-=2;


            EditorGUI.EndChangeCheck();

            newPos.width = position.width + FetchLabelSize(k_IndentAsString).x;
            newPos.x = position.x - FetchLabelSize(k_IndentAsString).x;
            EditorGUI.LabelField(newPos, "", GUI.skin.horizontalSlider);
            DrawSpace(ref newPos);

        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label, true) * 1.2f;

            if(property.isExpanded && property.objectReferenceValue)
            {
                height += GetBonusHeight(property);
            }

            if (!property.isExpanded && PropertyIsFirstInArray(property))
            {
                height += FetchButtonSize("").y + k_Spacing * 2f;
            }

            return height + k_HorizontalRuleHeight;
        }

       

        private float GetBonusHeight(SerializedProperty property)
        {
            float bonusHeight =
                +FetchObjectFieldSize("").y
                + FetchButtonSize("").y
                + FetchToggleSize("").y
                + k_Spacing * 4f;

            if(m_ShowOptions)
            {
                bonusHeight += FetchLabelSize("").y * 5f + FetchEnumFieldSize("").y + FetchToggleSize("").y;
                bonusHeight += k_Spacing * 8f;
                if (m_HasHeader)
                {
                    bonusHeight += FetchLabelSize("").y * 2f;
                }
            }

            if(PropertyIsFirstInArray(property))
            {
                bonusHeight += k_Spacing * 2f + FetchButtonSize("").y;
            }

            return bonusHeight;
        }

        private bool PropertyIsFirstInArray(SerializedProperty property)
        {
            return property.propertyPath.Contains("Array.data[")
                && property.propertyPath[property.propertyPath.Length - 2] == '0';
        }

        private string GetPropertyArrayName(SerializedProperty property)
        {
            return property.propertyPath.Remove(property.propertyPath.Length - (".Array.data[".Length + 2));
        }


        private bool ShouldDrawAttachButton(out RectTransform rect, Inventory inventory)
        {
            rect = null;

            GameObject selection = Selection.activeGameObject;
            bool shouldDraw = (selection && selection.scene.IsValid()
            && (!inventory.UIObject || (inventory.UIObject && !selection.transform.IsChildOf(inventory.UIObject.transform))) 
            && selection.TryGetComponent(out rect));

            return shouldDraw;
        }

        private bool ShouldDrawGenerateButton(Inventory inventory)
        {
            return !inventory.UIObject;
        }

        private bool ShouldDrawRegenerateButton(Inventory inventory)
        {
            return inventory.UIObject;
        }

        private Vector2 FetchLabelSize(string content)
        {
            GUIContent guiContent = new GUIContent(content);
            return new GUIStyle(GUI.skin.label).CalcSize(guiContent);
        }

        private Vector2 FetchButtonSize(string content)
        {
            GUIContent guiContent = new GUIContent(content);
            return new GUIStyle(GUI.skin.button).CalcSize(guiContent);
        }

        public Vector2 FetchToggleSize(string content)
        {
            GUIContent guiContent = new GUIContent(content);
            return new GUIStyle(GUI.skin.toggle).CalcSize(guiContent);
        }

        private Vector2 FetchObjectFieldSize(string content)
        {
            GUIContent buildContent = new GUIContent(content );
            Vector2 size = new GUIStyle("ObjectField").CalcSize(buildContent);
            return size;
        }

        private Vector2 FetchEnumFieldSize(string content)
        {
            GUIContent buildContent = new GUIContent(content);
            Vector2 size = new GUIStyle(GUI.skin.label).CalcSize(buildContent);
            return size;
        }

        private bool DrawDropDown(ref Rect position, bool value, ref float angle)
        {
            Vector2 dropdownSize = new Vector2(k_DropdownWidth, FetchObjectFieldSize(string.Format("None ({0})", nameof(Inventory))).y);
            Rect newPos = EditorGUI.IndentedRect(position);
            newPos.width = dropdownSize.x;
            newPos.height = dropdownSize.y;
            Vector2 pos = new Vector2(newPos.x + newPos.width * 0.5f, newPos.y + newPos.height * 0.5f);
            angle = value ? 0 : -90;
            GUIUtility.RotateAroundPivot(angle, pos);

            if (EditorGUI.DropdownButton(newPos, EditorGUIUtility.IconContent("icon dropdown"),
                FocusType.Keyboard, new GUIStyle(GUI.skin.label)))
            {
                value = !value;
                GUI.changed = true;
            }
            GUIUtility.RotateAroundPivot(-angle, pos);

            EditorGUI.indentLevel++; 
            return value;
        }

        private void DrawSpace(ref Rect position)
        {
            Rect pos = position;
            pos.height = k_Spacing;
            GUI.Label(pos, "");
            position.y += k_Spacing;
        }
    }
}