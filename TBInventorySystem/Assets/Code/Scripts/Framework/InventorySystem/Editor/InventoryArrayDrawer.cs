using UnityEditor;
using UnityEngine;

namespace InventorySystem
{
    [CustomPropertyDrawer(typeof(Inventory[]))]
    public class InventoryArrayDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Debug.Log("Hello");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label, true);

            return height;
        }
    }
}