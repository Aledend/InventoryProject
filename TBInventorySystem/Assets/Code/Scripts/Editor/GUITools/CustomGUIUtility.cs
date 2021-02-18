using UnityEditor;
using UnityEngine;

public class CustomGUIUtility : MonoBehaviour
{
    public static string TextFieldWithPlaceHolder(in string placeHolder, in string controlID, in string value, bool expandWidth)
    {
        GUIContent searchForItemsContent = new GUIContent(placeHolder);
        Rect searchForItemsRect = GUILayoutUtility.GetRect(searchForItemsContent, new GUIStyle(GUI.skin.textField), GUILayout.ExpandWidth(expandWidth));

        GUI.SetNextControlName(controlID);


        if (!string.IsNullOrEmpty(value) || GUI.GetNameOfFocusedControl() == controlID)
        {
            return GUI.TextField(searchForItemsRect, value);
        }
        else
        {
            GUI.TextField(searchForItemsRect, placeHolder);
        }

        EditorGUIUtility.AddCursorRect(searchForItemsRect, MouseCursor.Text);
        return value;
    }
}
