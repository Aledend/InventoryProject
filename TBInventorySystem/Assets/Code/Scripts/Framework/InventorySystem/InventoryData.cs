using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR


[CreateAssetMenu(fileName = "InventoryData", menuName ="ScriptableObjects/InventorySystem/InventoryData")]
public class InventoryData : ScriptableObject
{
    [SerializeField] private GameObject m_BackgroundPrefab = null;
    [SerializeField] private GameObject m_SlotPrefab = null;
    [SerializeField] private GameObject m_CanvasPrefab = null;
    [SerializeField] private GameObject m_HeaderPrefab = null;

    [SerializeField] [HideInInspector] private GameObject m_SceneTarget = null;
    [SerializeField] private string m_SceneTargetName = "InventoryUI";
    [SerializeField] [HideInInspector] private Canvas m_InventoryCanvas = null;

    public GameObject CreateBackground(Transform parent) => Instantiate(m_BackgroundPrefab, parent);
    public GameObject CreateSlot(Transform parent) => Instantiate(m_SlotPrefab, parent);
    public GameObject CreateHeader(Transform parent) => Instantiate(m_HeaderPrefab, parent);
    private GameObject SceneTarget => m_SceneTarget == null
        ? m_SceneTarget = new GameObject(m_SceneTargetName)
        : m_SceneTarget;
    public Canvas InventoryCanvas => m_InventoryCanvas == null
        ? m_InventoryCanvas = Instantiate(m_CanvasPrefab, SceneTarget.transform).GetComponent<Canvas>()
        : m_InventoryCanvas;

    private void OnEnable()
    {
        Assert.IsTrue(m_BackgroundPrefab.scene.name == null, "Inventory Background is not set.");
        Assert.IsTrue(m_SlotPrefab.scene.name == null, "Inventory Slot is not set");
        Assert.IsTrue(m_CanvasPrefab.scene.name == null, "Inventory Canvas is not set.");
        Assert.IsTrue(m_HeaderPrefab.scene.name == null, "Header is not set");
    }

}
