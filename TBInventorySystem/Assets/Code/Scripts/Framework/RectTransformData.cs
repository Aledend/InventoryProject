using UnityEngine;

public class RectTransformData
{
    private Vector3 m_LocalPosition;
    private Vector2 m_AnchoredPosition;
    private Vector2 m_SizeDelta;
    private Vector2 m_AnchorMin;
    private Vector2 m_AnchorMax;
    private Vector2 m_Pivot;
    private Vector3 m_Scale;
    private Quaternion m_Rotation;

    public RectTransformData(RectTransform rectTransform)
    {
        m_LocalPosition = rectTransform.localPosition;
        m_AnchoredPosition = rectTransform.anchoredPosition;
        m_SizeDelta = rectTransform.sizeDelta;
        m_AnchorMin = rectTransform.anchorMin;
        m_AnchorMax = rectTransform.anchorMax;
        m_Pivot = rectTransform.pivot;
        m_Scale = rectTransform.localScale;
        m_Rotation = rectTransform.localRotation;
    }

    public void WriteToRect(in RectTransform rectTransform, bool writeSize = true)
    {
        rectTransform.localPosition = m_LocalPosition;
        rectTransform.anchoredPosition = m_AnchoredPosition;
        if(writeSize)
            rectTransform.sizeDelta = m_SizeDelta;
        rectTransform.anchorMin = m_AnchorMin;
        rectTransform.anchorMax = m_AnchorMax;
        rectTransform.pivot = m_Pivot;
        rectTransform.localScale = m_Scale;
        rectTransform.localRotation = m_Rotation;
    }
}
