﻿using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[AddComponentMenu("UI/ScrollRectEx", 0x25), SelectionBase, ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
public class ScrollRectEx : UIBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, ICanvasElement
{
    [NonSerialized]
    private RectTransform m_Rect;
    [SerializeField]
    private RectTransform m_Viewport;
    private RectTransform m_ViewRect;
    private Bounds m_ViewBounds;
    private Bounds m_PrevViewBounds;
    [SerializeField]
    private RectTransform m_Content;
    protected Bounds m_ContentBounds;
    private Bounds m_PrevContentBounds;
    private readonly Vector3[] m_Corners = new Vector3[4];
    [SerializeField]
    private bool m_Horizontal = true;
    [SerializeField]
    private bool m_Vertical = true;
    private bool m_Dragging;
    private Vector2 m_PrevPosition = Vector2.zero;
    protected Vector2 m_ContentStartPosition = Vector2.zero;
    private Vector2 m_PointerStartLocalCursor = Vector2.zero;
    [NonSerialized]
    private bool m_HasRebuiltLayout = false;

    protected ScrollRectEx()
    {
    }

    internal static void AdjustBounds(ref Bounds viewBounds, ref Vector2 contentPivot, ref Vector3 contentSize, ref Vector3 contentPos)
    {
        Vector3 vector = viewBounds.size - contentSize;
        if (vector.x > 0f)
        {
            contentPos.x -= vector.x * (contentPivot.x - 0.5f);
            contentSize.x = viewBounds.size.x;
        }
        if (vector.y > 0f)
        {
            contentPos.y -= vector.y * (contentPivot.y - 0.5f);
            contentSize.y = viewBounds.size.y;
        }
    }

    private Vector2 CalculateOffset(Vector2 delta) =>
        InternalCalculateOffset(ref m_ViewBounds, ref m_ContentBounds, m_Horizontal, m_Vertical, ref delta);

    private void EnsureLayoutHasRebuilt()
    {
        if (!m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
        {
            Canvas.ForceUpdateCanvases();
        }
    }

    private Bounds GetBounds()
    {
        if (m_Content == null)
        {
            return new Bounds();
        }
        m_Content.GetWorldCorners(m_Corners);   //拿到content的四个角的世界坐标
        Matrix4x4 worldToLocalMatrix = viewRect.worldToLocalMatrix; //拿到viewRect的世界空间—本地空间的转换矩阵
        return InternalGetBounds(m_Corners, ref worldToLocalMatrix);
    }

    public virtual void GraphicUpdateComplete()
    {
    }

    internal static Vector2 InternalCalculateOffset(ref Bounds viewBounds, ref Bounds contentBounds, bool horizontal, bool vertical, ref Vector2 delta)
    {
        Vector2 zero = Vector2.zero;
        Vector2 min = contentBounds.min;
        Vector2 max = contentBounds.max;
        if (horizontal)
        {
            min.x += delta.x;
            max.x += delta.x;
            if (min.x > viewBounds.min.x)
            {
                zero.x = viewBounds.min.x - min.x;
            }
            else if (max.x < viewBounds.max.x)
            {
                zero.x = viewBounds.max.x - max.x;
            }
        }
        if (vertical)
        {
            min.y += delta.y;
            max.y += delta.y;
            if (max.y < viewBounds.max.y)
            {
                zero.y = viewBounds.max.y - max.y;
            }
            else if (min.y > viewBounds.min.y)
            {
                zero.y = viewBounds.min.y - min.y;
            }
        }
        return zero;
    }

    /// <summary>
    /// 根据一个矩形的四个角的世界坐标数组计算囊括数组所有条目的Bounds
    /// </summary>
    /// <param name="corners"></param>
    /// <param name="viewWorldToLocalMatrix"></param>
    /// <returns></returns>
    internal static Bounds InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix)
    {
        Vector3 rhs = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < 4; i++)
        {
            Vector3 lhs = viewWorldToLocalMatrix.MultiplyPoint3x4(corners[i]);  //把content的四个角的世界坐标转换为相对于viewport的本地坐标
            rhs = Vector3.Min(lhs, rhs);    //从四个角本地坐标中拿到最左下角的坐标赋给rhs
            vector2 = Vector3.Max(lhs, vector2);    //最右上角的坐标赋给vector2
        }
        Bounds bounds = new Bounds(rhs, Vector3.zero);  //新建一个Bounds，中心是rhs，即viewport左下角
        bounds.Encapsulate(vector2);    //囊括住vector2，这样Bounds的center就是rhs和vector2的中点了，而这个Bounds的左下角就是rhs，右上角就是vector2
        return bounds;
    }

    public override bool IsActive() => base.IsActive() && (m_Content != null);

    public virtual void LayoutComplete()
    {
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        SetDirty();
    }
    protected override void OnDisable()
    {
        CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
        m_HasRebuiltLayout = false;
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        base.OnDisable();
    }

    public virtual void OnBeginDrag(PointerEventData eventData)
    {
        if ((eventData.button == PointerEventData.InputButton.Left) && IsActive())
        {
            UpdateBounds();
            m_PointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out m_PointerStartLocalCursor);
            m_ContentStartPosition = m_Content.anchoredPosition;
            m_Dragging = true;
        }
    }
    public virtual void OnDrag(PointerEventData eventData)
    {
        Vector2 vector;
        if (((eventData.button == PointerEventData.InputButton.Left) && IsActive()) && RectTransformUtility.ScreenPointToLocalPointInRectangle(viewRect, eventData.position, eventData.pressEventCamera, out vector))
        {
            UpdateBounds();
            Vector2 vector2 = vector - m_PointerStartLocalCursor;
            Vector2 position = m_ContentStartPosition + vector2;
            Vector2 vector4 = CalculateOffset(position - m_Content.anchoredPosition);
            position += vector4;
            SetContentAnchoredPosition(position);
        }
    }
    public virtual void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            m_Dragging = false;
        }
    }

    protected virtual void LateUpdate()
    {
        if (m_Content != null)
        {
            EnsureLayoutHasRebuilt();
            UpdateBounds();
            float unscaledDeltaTime = Time.unscaledDeltaTime;
            Vector2 offset = CalculateOffset(Vector2.zero);
            if (!m_Dragging && (offset != Vector2.zero))
            {
                Vector2 anchoredPosition = m_Content.anchoredPosition;
                offset = CalculateOffset(anchoredPosition - m_Content.anchoredPosition);
                anchoredPosition += offset;
                SetContentAnchoredPosition(anchoredPosition);
            }
            if ((m_ViewBounds != m_PrevViewBounds) || (m_ContentBounds != m_PrevContentBounds) || (m_Content.anchoredPosition != m_PrevPosition))
            {
                UISystemProfilerApi.AddMarker("ScrollRectEx.value", this);
                UpdatePrevData();
            }
        }
    }

    protected override void OnRectTransformDimensionsChange()
    {
        SetDirty();
    }

    protected override void OnValidate()
    {
        SetDirtyCaching();
    }

    public virtual void Rebuild(CanvasUpdate executing)
    {
        if (executing == CanvasUpdate.PostLayout)
        {
            UpdateBounds();
            UpdatePrevData();
            m_HasRebuiltLayout = true;
        }
    }

    protected virtual void SetContentAnchoredPosition(Vector2 position)
    {
        if (!m_Horizontal)
        {
            position.x = m_Content.anchoredPosition.x;
        }
        if (!m_Vertical)
        {
            position.y = m_Content.anchoredPosition.y;
        }
        if (position != m_Content.anchoredPosition)
        {
            m_Content.anchoredPosition = position;
            UpdateBounds();
        }
    }

    protected void SetDirty()
    {
        if (IsActive())
        {
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
    }

    protected void SetDirtyCaching()
    {
        if (IsActive())
        {
            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }
    }

    protected void UpdateBounds()
    {
        m_ViewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);    //根据viewRect的center和size实例一个Bounds
        m_ContentBounds = GetBounds();  //计算content在viewport空间下的Bounds
        if (m_Content != null)
        {
            Vector3 size = m_ContentBounds.size;
            Vector3 center = m_ContentBounds.center;
            Vector2 pivot = m_Content.pivot;
            AdjustBounds(ref m_ViewBounds, ref pivot, ref size, ref center);
            m_ContentBounds.size = size;
            m_ContentBounds.center = center;
            Vector2 zero = Vector2.zero;
            if (m_ViewBounds.max.x > m_ContentBounds.max.x)
            {
                zero.x = Math.Min(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
            }
            else if (m_ViewBounds.min.x < m_ContentBounds.min.x)
            {
                zero.x = Math.Max(m_ViewBounds.min.x - m_ContentBounds.min.x, m_ViewBounds.max.x - m_ContentBounds.max.x);
            }
            if (m_ViewBounds.min.y < m_ContentBounds.min.y)
            {
                zero.y = Math.Max(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
            }
            else if (m_ViewBounds.max.y > m_ContentBounds.max.y)
            {
                zero.y = Math.Min(m_ViewBounds.min.y - m_ContentBounds.min.y, m_ViewBounds.max.y - m_ContentBounds.max.y);
            }
            if (zero.sqrMagnitude > float.Epsilon)
            {
                center = m_Content.anchoredPosition + zero;
                if (!m_Horizontal)
                {
                    center.x = m_Content.anchoredPosition.x;
                }
                if (!m_Vertical)
                {
                    center.y = m_Content.anchoredPosition.y;
                }
                AdjustBounds(ref m_ViewBounds, ref pivot, ref size, ref center);
            }
        }
    }

    protected void UpdatePrevData()
    {
        if (m_Content == null)
        {
            m_PrevPosition = Vector2.zero;
        }
        else
        {
            m_PrevPosition = m_Content.anchoredPosition;
        }
        m_PrevViewBounds = m_ViewBounds;
        m_PrevContentBounds = m_ContentBounds;
    }

    public RectTransform content
    {
        get =>
            m_Content;
        set
        {
            m_Content = value;
        }
    }

    public bool horizontal
    {
        get =>
            m_Horizontal;
        set
        {
            m_Horizontal = value;
        }
    }
    public bool vertical
    {
        get =>
            m_Vertical;
        set
        {
            m_Vertical = value;
        }
    }

    private RectTransform rectTransform
    {
        get
        {
            if (m_Rect == null)
            {
                m_Rect = base.GetComponent<RectTransform>();
            }
            return m_Rect;
        }
    }

    public RectTransform viewport
    {
        get =>
            m_Viewport;
        set
        {
            m_Viewport = value;
            SetDirtyCaching();
        }
    }
    protected RectTransform viewRect
    {
        get
        {
            if (m_ViewRect == null)
            {
                m_ViewRect = m_Viewport;
            }
            if (m_ViewRect == null)
            {
                m_ViewRect = (RectTransform)base.transform;
            }
            return m_ViewRect;
        }
    }
}