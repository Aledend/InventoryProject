using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Assertions;

public class HeaderInteraction : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform m_TransformToDrag = null;
    private bool m_Dragging = false;
    private IEnumerator m_DragCoroutine;
    private Vector2 m_DragOffset = Vector2.zero;

    private void Awake()
    {
        m_DragCoroutine = HandleDrag();
        Assert.IsNotNull(m_TransformToDrag);
    }

    public void BindDragTarget(RectTransform parentToBind)
    {
        m_TransformToDrag = parentToBind;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        m_DragOffset = GetMousePos() - m_TransformToDrag.anchoredPosition;
        m_Dragging = true;
        StopCoroutine(m_DragCoroutine);
        m_DragCoroutine = HandleDrag();
        StartCoroutine(m_DragCoroutine);
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        StopCoroutine(m_DragCoroutine);
        m_Dragging = false;
    }

    private IEnumerator HandleDrag()
    {
        while(m_Dragging)
        {
            m_TransformToDrag.anchoredPosition = GetMousePos() - m_DragOffset;
            yield return new WaitForEndOfFrame();
        }
    }

    private Vector2 GetMousePos()
    {
        return (Vector2)Input.mousePosition - new Vector2(Screen.width, Screen.height) * 0.5f;
    }
}