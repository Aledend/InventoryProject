using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor.Events;
#endif
namespace InventorySystem
{
    [CreateAssetMenu(fileName = "InventoryData", menuName = "ScriptableObjects/InventorySystem/InventoryData")]
    public class InventoryData : ScriptableObject
    {
        [SerializeField] private GameObject m_BackgroundPrefab = null;
        [SerializeField] private GameObject m_SlotPrefab = null;
        [SerializeField] private GameObject m_CanvasPrefab = null;
        [SerializeField] private GameObject m_HeaderPrefab = null;
        [SerializeField] private GameObject m_InventoryGroupPrefab = null;

        [SerializeField] [HideInInspector] private GameObject m_SceneTarget = null;
        [SerializeField] private string m_SceneTargetName = "InventoryUI";
        [SerializeField] [HideInInspector] private Canvas m_InventoryCanvas = null;


        #region Sprites
        [SerializeField] private Sprite m_SlotHoverSprite = null;
        [SerializeField] private Sprite m_SlotBackgroundSprite = null;
        [SerializeField] private Sprite m_SlotEmptySprite = null;

        public Sprite SlotHoverSprite => m_SlotHoverSprite;
        public Sprite SlotBackgroundSprite => m_SlotBackgroundSprite;
        public Sprite SlotEmptySprite => m_SlotEmptySprite;
        #endregion // Sprites

        public GameObject CreateBackground(Transform parent) => Instantiate(m_BackgroundPrefab, parent);
        public GameObject CreateSlot(Transform parent) => Instantiate(m_SlotPrefab, parent);
        public GameObject CreateHeader(Transform parent) => Instantiate(m_HeaderPrefab, parent);
        public GameObject CreateInventoryGroup(Transform parent) => Instantiate(m_InventoryGroupPrefab, parent);
        private GameObject SceneTarget => m_SceneTarget == null
            ? m_SceneTarget = new GameObject(m_SceneTargetName)
            : m_SceneTarget;

        public Canvas InventoryCanvas {
            get
            {
                if (!m_InventoryCanvas)
                {
                    m_InventoryCanvas = Instantiate(m_CanvasPrefab, SceneTarget.transform).GetComponent<Canvas>();
                    #region Reference Handling
#if UNITY_EDITOR
                    Framework.InventorySceneReference sceneRef = m_InventoryCanvas.gameObject
                        .AddComponent<Framework.InventorySceneReference>();
                    UnityEventTools.AddPersistentListener(sceneRef.ReturnObject, ReturnInventoryCanvas);
                    sceneRef.ReturnObject.SetPersistentListenerState(0, UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
#endif
#endregion
                }
                return m_InventoryCanvas;
            }
        }

        #region Reference Handling
        /// <summary>
        /// Sent to generated canvas to keep reference to scene object alive.
        /// </summary>
        /// <param name="canvas">The stored scene reference.</param>
        public void ReturnInventoryCanvas(Framework.InventorySceneReference sceneRef, GameObject canvas)
        {
            if (m_InventoryCanvas == null)
            {
                m_InventoryCanvas = canvas.GetComponent<Canvas>();
            }
            else if (m_InventoryCanvas != canvas.GetComponent<Canvas>()) 
            {
                if (Application.isPlaying)
                    Destroy(sceneRef);
                else
                    DestroyImmediate(sceneRef);
            }
        }
        #endregion
    }
}