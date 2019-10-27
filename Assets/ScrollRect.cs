namespace UnityEngine.UI.Ext
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;

    [AddComponentMenu("UI/Scroll Rect", 0x25), SelectionBase, ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public class ScrollRect : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, ICanvasElement, ILayoutElement, ILayoutGroup, IEventSystemHandler, ILayoutController
    {
        [SerializeField]
        private RectTransform m_Content;
        protected Bounds m_ContentBounds;
        protected Vector2 m_ContentStartPosition = Vector2.zero;
        private readonly Vector3[] m_Corners = new Vector3[4];
        private bool m_Dragging;
        [NonSerialized]
        private bool m_HasRebuiltLayout = false;
        [SerializeField]
        private bool m_Horizontal = true;
        [SerializeField]
        private Scrollbar m_HorizontalScrollbar;
        private RectTransform m_HorizontalScrollbarRect;
        [SerializeField]
        private float m_HorizontalScrollbarSpacing;
        [SerializeField]
        private ScrollbarVisibility m_HorizontalScrollbarVisibility;
        private bool m_HSliderExpand;
        private float m_HSliderHeight;
        [SerializeField]
        private ScrollRectEvent m_OnValueChanged = new ScrollRectEvent();
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        private Bounds m_PrevContentBounds;
        private Vector2 m_PrevPosition = Vector2.zero;
        private Bounds m_PrevViewBounds;
        [NonSerialized]
        private RectTransform m_Rect;
        private DrivenRectTransformTracker m_Tracker;
        private Vector2 m_Velocity;
        [SerializeField]
        private bool m_Vertical = true;
        [SerializeField]
        private Scrollbar m_VerticalScrollbar;
        private RectTransform m_VerticalScrollbarRect;
        [SerializeField]
        private float m_VerticalScrollbarSpacing;
        [SerializeField]
        private ScrollbarVisibility m_VerticalScrollbarVisibility;
        private Bounds m_ViewBounds;
        [SerializeField]
        private RectTransform m_Viewport;
        private RectTransform m_ViewRect;
        private bool m_VSliderExpand;
        private float m_VSliderWidth;

        protected ScrollRect()
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

        public virtual void CalculateLayoutInputHorizontal()
        {
        }

        public virtual void CalculateLayoutInputVertical()
        {
        }

        private Vector2 CalculateOffset(Vector2 delta) =>
            InternalCalculateOffset(ref this.m_ViewBounds, ref this.m_ContentBounds, this.m_Horizontal, this.m_Vertical, ref delta);

        private void EnsureLayoutHasRebuilt()
        {
            if (!this.m_HasRebuiltLayout && !CanvasUpdateRegistry.IsRebuildingLayout())
            {
                Canvas.ForceUpdateCanvases();
            }
        }

        private Bounds GetBounds()
        {
            if (this.m_Content == null)
            {
                return new Bounds();
            }
            this.m_Content.GetWorldCorners(this.m_Corners);
            Matrix4x4 worldToLocalMatrix = this.viewRect.worldToLocalMatrix;
            return InternalGetBounds(this.m_Corners, ref worldToLocalMatrix);
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

        internal static Bounds InternalGetBounds(Vector3[] corners, ref Matrix4x4 viewWorldToLocalMatrix)
        {
            Vector3 rhs = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                Vector3 lhs = viewWorldToLocalMatrix.MultiplyPoint3x4(corners[i]);
                rhs = Vector3.Min(lhs, rhs);
                vector2 = Vector3.Max(lhs, vector2);
            }
            Bounds bounds = new Bounds(rhs, Vector3.zero);
            bounds.Encapsulate(vector2);
            return bounds;
        }

        public override bool IsActive() =>
            (base.IsActive() && (this.m_Content != null));

        protected virtual void LateUpdate()
        {
            if (this.m_Content != null)
            {
                this.EnsureLayoutHasRebuilt();
                this.UpdateBounds();
                float unscaledDeltaTime = Time.unscaledDeltaTime;
                Vector2 offset = this.CalculateOffset(Vector2.zero);
                if (!this.m_Dragging && ((offset != Vector2.zero) || (this.m_Velocity != Vector2.zero)))
                {
                    Vector2 anchoredPosition = this.m_Content.anchoredPosition;
                    for (int i = 0; i < 2; i++)
                    {
                        this.m_Velocity[i] = 0f;
                    }
                    offset = this.CalculateOffset(anchoredPosition - this.m_Content.anchoredPosition);
                    anchoredPosition += offset;
                    this.SetContentAnchoredPosition(anchoredPosition);
                }
                if (this.m_Dragging) // && this.m_Inertia
                {
                    Vector3 b = (Vector3)((this.m_Content.anchoredPosition - this.m_PrevPosition) / unscaledDeltaTime);
                    this.m_Velocity = Vector3.Lerp((Vector3)this.m_Velocity, b, unscaledDeltaTime * 10f);
                }
                if (((this.m_ViewBounds != this.m_PrevViewBounds) || (this.m_ContentBounds != this.m_PrevContentBounds)) || (this.m_Content.anchoredPosition != this.m_PrevPosition))
                {
                    this.UpdateScrollbars(offset);
                    UISystemProfilerApi.AddMarker("ScrollRect.value", this);
                    this.m_OnValueChanged.Invoke(this.normalizedPosition);
                    this.UpdatePrevData();
                }
                this.UpdateScrollbarVisibility();
            }
        }

        public virtual void LayoutComplete()
        {
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if ((eventData.button == PointerEventData.InputButton.Left) && this.IsActive())
            {
                this.UpdateBounds();
                this.m_PointerStartLocalCursor = Vector2.zero;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(this.viewRect, eventData.position, eventData.pressEventCamera, out this.m_PointerStartLocalCursor);
                this.m_ContentStartPosition = this.m_Content.anchoredPosition;
                this.m_Dragging = true;
            }
        }

        protected override void OnDisable()
        {
            CanvasUpdateRegistry.UnRegisterCanvasElementForRebuild(this);
            if (this.m_HorizontalScrollbar != null)
            {
                this.m_HorizontalScrollbar.onValueChanged.RemoveListener(new UnityAction<float>(this.SetHorizontalNormalizedPosition));
            }
            if (this.m_VerticalScrollbar != null)
            {
                this.m_VerticalScrollbar.onValueChanged.RemoveListener(new UnityAction<float>(this.SetVerticalNormalizedPosition));
            }
            this.m_HasRebuiltLayout = false;
            this.m_Tracker.Clear();
            this.m_Velocity = Vector2.zero;
            LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
            base.OnDisable();
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            Vector2 vector;
            if (((eventData.button == PointerEventData.InputButton.Left) && this.IsActive()) && RectTransformUtility.ScreenPointToLocalPointInRectangle(this.viewRect, eventData.position, eventData.pressEventCamera, out vector))
            {
                this.UpdateBounds();
                Vector2 vector2 = vector - this.m_PointerStartLocalCursor;
                Vector2 position = this.m_ContentStartPosition + vector2;
                Vector2 vector4 = this.CalculateOffset(position - this.m_Content.anchoredPosition);
                position += vector4;
                this.SetContentAnchoredPosition(position);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (this.m_HorizontalScrollbar != null)
            {
                this.m_HorizontalScrollbar.onValueChanged.AddListener(new UnityAction<float>(this.SetHorizontalNormalizedPosition));
            }
            if (this.m_VerticalScrollbar != null)
            {
                this.m_VerticalScrollbar.onValueChanged.AddListener(new UnityAction<float>(this.SetVerticalNormalizedPosition));
            }
            CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
            this.SetDirty();
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                this.m_Dragging = false;
            }
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                this.m_Velocity = Vector2.zero;
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            this.SetDirty();
        }

        protected override void OnValidate()
        {
            this.SetDirtyCaching();
        }

        public virtual void Rebuild(CanvasUpdate executing)
        {
            if (executing == CanvasUpdate.Prelayout)
            {
                this.UpdateCachedData();
            }
            if (executing == CanvasUpdate.PostLayout)
            {
                this.UpdateBounds();
                this.UpdateScrollbars(Vector2.zero);
                this.UpdatePrevData();
                this.m_HasRebuiltLayout = true;
            }
        }

        private static float RubberDelta(float overStretching, float viewSize) =>
            (((1f - (1f / (((Mathf.Abs(overStretching) * 0.55f) / viewSize) + 1f))) * viewSize) * Mathf.Sign(overStretching));

        protected virtual void SetContentAnchoredPosition(Vector2 position)
        {
            if (!this.m_Horizontal)
            {
                position.x = this.m_Content.anchoredPosition.x;
            }
            if (!this.m_Vertical)
            {
                position.y = this.m_Content.anchoredPosition.y;
            }
            if (position != this.m_Content.anchoredPosition)
            {
                this.m_Content.anchoredPosition = position;
                this.UpdateBounds();
            }
        }

        protected void SetDirty()
        {
            if (this.IsActive())
            {
                LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
            }
        }

        protected void SetDirtyCaching()
        {
            if (this.IsActive())
            {
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
                LayoutRebuilder.MarkLayoutForRebuild(this.rectTransform);
            }
        }

        private void SetHorizontalNormalizedPosition(float value)
        {
            this.SetNormalizedPosition(value, 0);
        }

        public virtual void SetLayoutHorizontal()
        {
            this.m_Tracker.Clear();
            if (this.m_HSliderExpand || this.m_VSliderExpand)
            {
                this.m_Tracker.Add(this, this.viewRect, DrivenTransformProperties.SizeDelta | DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition);
                this.viewRect.anchorMin = Vector2.zero;
                this.viewRect.anchorMax = Vector2.one;
                this.viewRect.sizeDelta = Vector2.zero;
                this.viewRect.anchoredPosition = Vector2.zero;
                LayoutRebuilder.ForceRebuildLayoutImmediate(this.content);
                this.m_ViewBounds = new Bounds((Vector3)this.viewRect.rect.center, (Vector3)this.viewRect.rect.size);
                this.m_ContentBounds = this.GetBounds();
            }
            if (this.m_VSliderExpand && this.vScrollingNeeded)
            {
                this.viewRect.sizeDelta = new Vector2(-(this.m_VSliderWidth + this.m_VerticalScrollbarSpacing), this.viewRect.sizeDelta.y);
                LayoutRebuilder.ForceRebuildLayoutImmediate(this.content);
                this.m_ViewBounds = new Bounds((Vector3)this.viewRect.rect.center, (Vector3)this.viewRect.rect.size);
                this.m_ContentBounds = this.GetBounds();
            }
            if (this.m_HSliderExpand && this.hScrollingNeeded)
            {
                this.viewRect.sizeDelta = new Vector2(this.viewRect.sizeDelta.x, -(this.m_HSliderHeight + this.m_HorizontalScrollbarSpacing));
                this.m_ViewBounds = new Bounds((Vector3)this.viewRect.rect.center, (Vector3)this.viewRect.rect.size);
                this.m_ContentBounds = this.GetBounds();
            }
            if ((this.m_VSliderExpand && this.vScrollingNeeded) && ((this.viewRect.sizeDelta.x == 0f) && (this.viewRect.sizeDelta.y < 0f)))
            {
                this.viewRect.sizeDelta = new Vector2(-(this.m_VSliderWidth + this.m_VerticalScrollbarSpacing), this.viewRect.sizeDelta.y);
            }
        }

        public virtual void SetLayoutVertical()
        {
            this.UpdateScrollbarLayout();
            this.m_ViewBounds = new Bounds((Vector3)this.viewRect.rect.center, (Vector3)this.viewRect.rect.size);
            this.m_ContentBounds = this.GetBounds();
        }

        protected virtual void SetNormalizedPosition(float value, int axis)
        {
            this.EnsureLayoutHasRebuilt();
            this.UpdateBounds();
            float num = this.m_ContentBounds.size[axis] - this.m_ViewBounds.size[axis];
            float num2 = this.m_ViewBounds.min[axis] - (value * num);
            float num3 = (this.m_Content.localPosition[axis] + num2) - this.m_ContentBounds.min[axis];
            Vector3 localPosition = this.m_Content.localPosition;
            if (Mathf.Abs((float)(localPosition[axis] - num3)) > 0.01f)
            {
                localPosition[axis] = num3;
                this.m_Content.localPosition = localPosition;
                this.m_Velocity[axis] = 0f;
                this.UpdateBounds();
            }
        }

        private void SetVerticalNormalizedPosition(float value)
        {
            this.SetNormalizedPosition(value, 1);
        }

        public virtual void StopMovement()
        {
            this.m_Velocity = Vector2.zero;
        }

        //Transform ICanvasElement.get_transform() =>
        //    base.transform;

        protected void UpdateBounds()
        {
            this.m_ViewBounds = new Bounds((Vector3)this.viewRect.rect.center, (Vector3)this.viewRect.rect.size);
            this.m_ContentBounds = this.GetBounds();
            if (this.m_Content != null)
            {
                Vector3 size = this.m_ContentBounds.size;
                Vector3 center = this.m_ContentBounds.center;
                Vector2 pivot = this.m_Content.pivot;
                AdjustBounds(ref this.m_ViewBounds, ref pivot, ref size, ref center);
                this.m_ContentBounds.size = size;
                this.m_ContentBounds.center = center;
                Vector2 zero = Vector2.zero;
                if (this.m_ViewBounds.max.x > this.m_ContentBounds.max.x)
                {
                    zero.x = Math.Min((float)(this.m_ViewBounds.min.x - this.m_ContentBounds.min.x), (float)(this.m_ViewBounds.max.x - this.m_ContentBounds.max.x));
                }
                else if (this.m_ViewBounds.min.x < this.m_ContentBounds.min.x)
                {
                    zero.x = Math.Max((float)(this.m_ViewBounds.min.x - this.m_ContentBounds.min.x), (float)(this.m_ViewBounds.max.x - this.m_ContentBounds.max.x));
                }
                if (this.m_ViewBounds.min.y < this.m_ContentBounds.min.y)
                {
                    zero.y = Math.Max((float)(this.m_ViewBounds.min.y - this.m_ContentBounds.min.y), (float)(this.m_ViewBounds.max.y - this.m_ContentBounds.max.y));
                }
                else if (this.m_ViewBounds.max.y > this.m_ContentBounds.max.y)
                {
                    zero.y = Math.Min((float)(this.m_ViewBounds.min.y - this.m_ContentBounds.min.y), (float)(this.m_ViewBounds.max.y - this.m_ContentBounds.max.y));
                }
                if (zero.sqrMagnitude > float.Epsilon)
                {
                    center = (Vector3)(this.m_Content.anchoredPosition + zero);
                    if (!this.m_Horizontal)
                    {
                        center.x = this.m_Content.anchoredPosition.x;
                    }
                    if (!this.m_Vertical)
                    {
                        center.y = this.m_Content.anchoredPosition.y;
                    }
                    AdjustBounds(ref this.m_ViewBounds, ref pivot, ref size, ref center);
                }
            }
        }

        private void UpdateCachedData()
        {
            Transform transform = base.transform;
            this.m_HorizontalScrollbarRect = (this.m_HorizontalScrollbar != null) ? (this.m_HorizontalScrollbar.transform as RectTransform) : null;
            this.m_VerticalScrollbarRect = (this.m_VerticalScrollbar != null) ? (this.m_VerticalScrollbar.transform as RectTransform) : null;
            bool flag = this.viewRect.parent == transform;
            bool flag2 = (this.m_HorizontalScrollbarRect == null) || (this.m_HorizontalScrollbarRect.parent == transform);
            bool flag3 = (this.m_VerticalScrollbarRect == null) || (this.m_VerticalScrollbarRect.parent == transform);
            bool flag4 = (flag && flag2) && flag3;
            this.m_HSliderExpand = (flag4 && (this.m_HorizontalScrollbarRect != null)) && (this.horizontalScrollbarVisibility == ScrollbarVisibility.AutoHideAndExpandViewport);
            this.m_VSliderExpand = (flag4 && (this.m_VerticalScrollbarRect != null)) && (this.verticalScrollbarVisibility == ScrollbarVisibility.AutoHideAndExpandViewport);
            this.m_HSliderHeight = (this.m_HorizontalScrollbarRect != null) ? this.m_HorizontalScrollbarRect.rect.height : 0f;
            this.m_VSliderWidth = (this.m_VerticalScrollbarRect != null) ? this.m_VerticalScrollbarRect.rect.width : 0f;
        }

        private static void UpdateOneScrollbarVisibility(bool xScrollingNeeded, bool xAxisEnabled, ScrollbarVisibility scrollbarVisibility, Scrollbar scrollbar)
        {
            if (scrollbar != null)
            {
                if (scrollbarVisibility == ScrollbarVisibility.Permanent)
                {
                    if (scrollbar.gameObject.activeSelf != xAxisEnabled)
                    {
                        scrollbar.gameObject.SetActive(xAxisEnabled);
                    }
                }
                else if (scrollbar.gameObject.activeSelf != xScrollingNeeded)
                {
                    scrollbar.gameObject.SetActive(xScrollingNeeded);
                }
            }
        }

        protected void UpdatePrevData()
        {
            if (this.m_Content == null)
            {
                this.m_PrevPosition = Vector2.zero;
            }
            else
            {
                this.m_PrevPosition = this.m_Content.anchoredPosition;
            }
            this.m_PrevViewBounds = this.m_ViewBounds;
            this.m_PrevContentBounds = this.m_ContentBounds;
        }

        private void UpdateScrollbarLayout()
        {
            if (this.m_VSliderExpand && (this.m_HorizontalScrollbar != null))
            {
                this.m_Tracker.Add(this, this.m_HorizontalScrollbarRect, DrivenTransformProperties.SizeDeltaX | DrivenTransformProperties.AnchorMaxX | DrivenTransformProperties.AnchorMinX | DrivenTransformProperties.AnchoredPositionX);
                this.m_HorizontalScrollbarRect.anchorMin = new Vector2(0f, this.m_HorizontalScrollbarRect.anchorMin.y);
                this.m_HorizontalScrollbarRect.anchorMax = new Vector2(1f, this.m_HorizontalScrollbarRect.anchorMax.y);
                this.m_HorizontalScrollbarRect.anchoredPosition = new Vector2(0f, this.m_HorizontalScrollbarRect.anchoredPosition.y);
                if (this.vScrollingNeeded)
                {
                    this.m_HorizontalScrollbarRect.sizeDelta = new Vector2(-(this.m_VSliderWidth + this.m_VerticalScrollbarSpacing), this.m_HorizontalScrollbarRect.sizeDelta.y);
                }
                else
                {
                    this.m_HorizontalScrollbarRect.sizeDelta = new Vector2(0f, this.m_HorizontalScrollbarRect.sizeDelta.y);
                }
            }
            if (this.m_HSliderExpand && (this.m_VerticalScrollbar != null))
            {
                this.m_Tracker.Add(this, this.m_VerticalScrollbarRect, DrivenTransformProperties.SizeDeltaY | DrivenTransformProperties.AnchorMaxY | DrivenTransformProperties.AnchorMinY | DrivenTransformProperties.AnchoredPositionY);
                this.m_VerticalScrollbarRect.anchorMin = new Vector2(this.m_VerticalScrollbarRect.anchorMin.x, 0f);
                this.m_VerticalScrollbarRect.anchorMax = new Vector2(this.m_VerticalScrollbarRect.anchorMax.x, 1f);
                this.m_VerticalScrollbarRect.anchoredPosition = new Vector2(this.m_VerticalScrollbarRect.anchoredPosition.x, 0f);
                if (this.hScrollingNeeded)
                {
                    this.m_VerticalScrollbarRect.sizeDelta = new Vector2(this.m_VerticalScrollbarRect.sizeDelta.x, -(this.m_HSliderHeight + this.m_HorizontalScrollbarSpacing));
                }
                else
                {
                    this.m_VerticalScrollbarRect.sizeDelta = new Vector2(this.m_VerticalScrollbarRect.sizeDelta.x, 0f);
                }
            }
        }

        private void UpdateScrollbars(Vector2 offset)
        {
            if (this.m_HorizontalScrollbar != null)
            {
                if (this.m_ContentBounds.size.x > 0f)
                {
                    this.m_HorizontalScrollbar.size = Mathf.Clamp01((this.m_ViewBounds.size.x - Mathf.Abs(offset.x)) / this.m_ContentBounds.size.x);
                }
                else
                {
                    this.m_HorizontalScrollbar.size = 1f;
                }
                this.m_HorizontalScrollbar.value = this.horizontalNormalizedPosition;
            }
            if (this.m_VerticalScrollbar != null)
            {
                if (this.m_ContentBounds.size.y > 0f)
                {
                    this.m_VerticalScrollbar.size = Mathf.Clamp01((this.m_ViewBounds.size.y - Mathf.Abs(offset.y)) / this.m_ContentBounds.size.y);
                }
                else
                {
                    this.m_VerticalScrollbar.size = 1f;
                }
                this.m_VerticalScrollbar.value = this.verticalNormalizedPosition;
            }
        }

        private void UpdateScrollbarVisibility()
        {
            UpdateOneScrollbarVisibility(this.vScrollingNeeded, this.m_Vertical, this.m_VerticalScrollbarVisibility, this.m_VerticalScrollbar);
            UpdateOneScrollbarVisibility(this.hScrollingNeeded, this.m_Horizontal, this.m_HorizontalScrollbarVisibility, this.m_HorizontalScrollbar);
        }

        public RectTransform content
        {
            get =>
                this.m_Content;
            set
            {
                this.m_Content = value;
            }
        }

        public virtual float flexibleHeight =>
            -1f;

        public virtual float flexibleWidth =>
            -1f;

        public bool horizontal
        {
            get =>
                this.m_Horizontal;
            set
            {
                this.m_Horizontal = value;
            }
        }

        public float horizontalNormalizedPosition
        {
            get
            {
                this.UpdateBounds();
                if ((this.m_ContentBounds.size.x <= this.m_ViewBounds.size.x) || Mathf.Approximately(this.m_ContentBounds.size.x, this.m_ViewBounds.size.x))
                {
                    return ((this.m_ViewBounds.min.x <= this.m_ContentBounds.min.x) ? ((float)0) : ((float)1));
                }
                return ((this.m_ViewBounds.min.x - this.m_ContentBounds.min.x) / (this.m_ContentBounds.size.x - this.m_ViewBounds.size.x));
            }
            set
            {
                this.SetNormalizedPosition(value, 0);
            }
        }

        public Scrollbar horizontalScrollbar
        {
            get =>
                this.m_HorizontalScrollbar;
            set
            {
                if (this.m_HorizontalScrollbar != null)
                {
                    this.m_HorizontalScrollbar.onValueChanged.RemoveListener(new UnityAction<float>(this.SetHorizontalNormalizedPosition));
                }
                this.m_HorizontalScrollbar = value;
                if (this.m_HorizontalScrollbar != null)
                {
                    this.m_HorizontalScrollbar.onValueChanged.AddListener(new UnityAction<float>(this.SetHorizontalNormalizedPosition));
                }
                this.SetDirtyCaching();
            }
        }

        public float horizontalScrollbarSpacing
        {
            get =>
                this.m_HorizontalScrollbarSpacing;
            set
            {
                this.m_HorizontalScrollbarSpacing = value;
                this.SetDirty();
            }
        }

        public ScrollbarVisibility horizontalScrollbarVisibility
        {
            get =>
                this.m_HorizontalScrollbarVisibility;
            set
            {
                this.m_HorizontalScrollbarVisibility = value;
                this.SetDirtyCaching();
            }
        }

        private bool hScrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                {
                    return (this.m_ContentBounds.size.x > (this.m_ViewBounds.size.x + 0.01f));
                }
                return true;
            }
        }

        public virtual int layoutPriority =>
            -1;

        public virtual float minHeight =>
            -1f;

        public virtual float minWidth =>
            -1f;

        public Vector2 normalizedPosition
        {
            get =>
                new Vector2(this.horizontalNormalizedPosition, this.verticalNormalizedPosition);
            set
            {
                this.SetNormalizedPosition(value.x, 0);
                this.SetNormalizedPosition(value.y, 1);
            }
        }

        public ScrollRectEvent onValueChanged
        {
            get =>
                this.m_OnValueChanged;
            set
            {
                this.m_OnValueChanged = value;
            }
        }

        public virtual float preferredHeight =>
            -1f;

        public virtual float preferredWidth =>
            -1f;

        private RectTransform rectTransform
        {
            get
            {
                if (this.m_Rect == null)
                {
                    this.m_Rect = base.GetComponent<RectTransform>();
                }
                return this.m_Rect;
            }
        }

        public Vector2 velocity
        {
            get =>
                this.m_Velocity;
            set
            {
                this.m_Velocity = value;
            }
        }

        public bool vertical
        {
            get =>
                this.m_Vertical;
            set
            {
                this.m_Vertical = value;
            }
        }

        public float verticalNormalizedPosition
        {
            get
            {
                this.UpdateBounds();
                if ((this.m_ContentBounds.size.y <= this.m_ViewBounds.size.y) || Mathf.Approximately(this.m_ContentBounds.size.y, this.m_ViewBounds.size.y))
                {
                    return ((this.m_ViewBounds.min.y <= this.m_ContentBounds.min.y) ? ((float)0) : ((float)1));
                }
                return ((this.m_ViewBounds.min.y - this.m_ContentBounds.min.y) / (this.m_ContentBounds.size.y - this.m_ViewBounds.size.y));
            }
            set
            {
                this.SetNormalizedPosition(value, 1);
            }
        }

        public Scrollbar verticalScrollbar
        {
            get =>
                this.m_VerticalScrollbar;
            set
            {
                if (this.m_VerticalScrollbar != null)
                {
                    this.m_VerticalScrollbar.onValueChanged.RemoveListener(new UnityAction<float>(this.SetVerticalNormalizedPosition));
                }
                this.m_VerticalScrollbar = value;
                if (this.m_VerticalScrollbar != null)
                {
                    this.m_VerticalScrollbar.onValueChanged.AddListener(new UnityAction<float>(this.SetVerticalNormalizedPosition));
                }
                this.SetDirtyCaching();
            }
        }

        public float verticalScrollbarSpacing
        {
            get =>
                this.m_VerticalScrollbarSpacing;
            set
            {
                this.m_VerticalScrollbarSpacing = value;
                this.SetDirty();
            }
        }

        public ScrollbarVisibility verticalScrollbarVisibility
        {
            get =>
                this.m_VerticalScrollbarVisibility;
            set
            {
                this.m_VerticalScrollbarVisibility = value;
                this.SetDirtyCaching();
            }
        }

        public RectTransform viewport
        {
            get =>
                this.m_Viewport;
            set
            {
                this.m_Viewport = value;
                this.SetDirtyCaching();
            }
        }

        protected RectTransform viewRect
        {
            get
            {
                if (this.m_ViewRect == null)
                {
                    this.m_ViewRect = this.m_Viewport;
                }
                if (this.m_ViewRect == null)
                {
                    this.m_ViewRect = (RectTransform)base.transform;
                }
                return this.m_ViewRect;
            }
        }

        private bool vScrollingNeeded
        {
            get
            {
                if (Application.isPlaying)
                {
                    return (this.m_ContentBounds.size.y > (this.m_ViewBounds.size.y + 0.01f));
                }
                return true;
            }
        }

        public enum ScrollbarVisibility
        {
            Permanent,
            AutoHide,
            AutoHideAndExpandViewport
        }

        [Serializable]
        public class ScrollRectEvent : UnityEvent<Vector2>
        {
        }
    }
}