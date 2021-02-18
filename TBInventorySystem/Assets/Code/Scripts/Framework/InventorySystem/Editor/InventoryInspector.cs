using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Inventory))]
[CanEditMultipleObjects]
public class InventoryInspector : Editor
{
    private Inventory m_Inventory = null;

    private void OnEnable()
    {
        m_Inventory = target as Inventory;    
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.BeginVertical();
        if(GUILayout.Button("Create UI"))
        {
            m_Inventory.BuildInventoryUI();
        }
        GUILayout.EndVertical();
    }
}
