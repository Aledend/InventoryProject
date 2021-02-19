using UnityEditor;
using UnityEngine;

namespace InventorySystem
{
    [CustomPropertyDrawer(typeof(Inventory))]
    public class InventoryDrawer : PropertyDrawer
    {

        private const float k_Spacing = 5f;
        private const string k_AttachComponent = "Attach Component";
        private const string k_CreateReferenceInScene = "Create Reference In Scene";
        private bool m_Expanded = true;
        private float m_DropDownAngle = -90f;
        private float m_OptionsAngle = -90f;
        private const float k_DropdownWidth = 15f;
        private const float k_ButtonOffset = 330f;
        private const float k_IndentOffset = 45f; // Simulate indent since EditorGUI.indentLevel wouldn't give in
        private bool m_ShowOptions = false;
        private bool m_HasHeader = false;
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect newPos = position;

            Inventory inventory = property.objectReferenceValue as Inventory;

            
            
            if (inventory)
            {
                DrawDropDown(ref position, ref m_Expanded, ref m_DropDownAngle);
                EditorGUI.indentLevel++;
            }


            EditorGUI.PropertyField(newPos, property, true);
            newPos.y += FetchObjectFieldSize("").y;



            DrawSpace(ref newPos);


            if (!m_Expanded || !inventory)
                return;



            Rect buttonRect = newPos;

            #region AttachToUIButton
            GameObject selection = Selection.activeGameObject;

            bool shouldDrawAttachButton = ShouldDrawAttachButton(out RectTransform rect);

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
                    inventory.UIParent = rect;
                    if (inventory.UIObject)
                    {
                        inventory.UIObject.transform.SetParent(inventory.UIParent);
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
                    Debug.Log("Generating");
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
            DrawDropDown(ref newPos, ref m_ShowOptions, ref m_OptionsAngle);
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

                    inventory.UIDragTarget = EditorGUI.ObjectField(newPos, nameof(inventory.UIDragTarget) +
                        (inventory.UIDragTarget ? string.Empty : " (Defaulted to self)"),
                        inventory.UIDragTarget, typeof(RectTransform), true) as RectTransform;
                    newPos.y += newPos.height;
                    DrawSpace(ref newPos);
                }
                inventory.HideOnPlay = EditorGUI.Toggle(newPos, "Hide on Play", inventory.HideOnPlay);
                newPos.y += FetchToggleSize("").y;
                DrawSpace(ref newPos);
                inventory.InventoryToggle = (KeyCode)EditorGUI.EnumPopup(newPos, "ToggleKey", inventory.InventoryToggle);
                newPos.y += FetchEnumFieldSize("").y;
                DrawSpace(ref newPos);
            }
            #endregion // Options

            EditorGUI.indentLevel--;

        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label, true);
            if(m_Expanded && property.objectReferenceValue)
            {
                height += GetBonusHeight();
            }
            return height;
        }

       

        private float GetBonusHeight()
        {
            float bonusHeight =
                k_Spacing
                + FetchObjectFieldSize("").y
                + k_Spacing
                + FetchButtonSize("").y // Generate/Regenerate button
                + k_Spacing
                + FetchToggleSize("").y
                + k_Spacing;

            if(m_ShowOptions)
            {
                bonusHeight += FetchLabelSize("").y * 5f + FetchEnumFieldSize("").y + FetchToggleSize("").y;
                bonusHeight += k_Spacing * 8f;
            }

            if(m_HasHeader)
            {
                bonusHeight += FetchLabelSize("").y * 2f;
            }

            return bonusHeight;
        }

        private bool ShouldDrawAttachButton(out RectTransform rect)
        {
            rect = null;

            GameObject selection = Selection.activeGameObject;
            bool shouldDraw = (selection && selection.scene.IsValid()
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

        private void DrawDropDown(ref Rect position, ref bool value, ref float angle)
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
            }
            GUIUtility.RotateAroundPivot(-angle, pos);

            position.x += dropdownSize.x;
            position.width -= dropdownSize.x;
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