namespace UnityEngine.UI.Ext
{
    using System;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.EventSystems;

    [AddComponentMenu("UI/Scroll Rect", 0x25), SelectionBase, ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public class ScrollRect : UIBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, ICanvasElement, ILayoutElement, IEventSystemHandler
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
        private ScrollRectEvent m_OnValueChanged = new ScrollRectEvent();
        private Vector2 m_PointerStartLocalCursor = Vector2.zero;
        private Bounds m_PrevContentBounds;
        private Vector2 m_PrevPosition = Vector2.zero;
        private Bounds m_PrevViewBounds;
        [NonSerialized]
        private RectTransform m_Rect;
        [SerializeField]
        private bool m_Vertical = true;
        private Bounds m_ViewBounds;
        [SerializeField]
        private RectTransform m_Viewport;
        private RectTransform m_ViewRect;

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
                if (!this.m_Dragging && ((offset != Vector2.zero)))
                {
                    Vector2 anchoredPosition = this.m_Content.anchoredPosition;
                    offset = this.CalculateOffset(anchoredPosition - this.m_Content.anchoredPosition);
                    anchoredPosition += offset;
                    this.SetContentAnchoredPosition(anchoredPosition);
                }
                if (((this.m_ViewBounds != this.m_PrevViewBounds) || (this.m_ContentBounds != this.m_PrevContentBounds)) || (this.m_Content.anchoredPosition != this.m_PrevPosition))
                {
                    UISystemProfilerApi.AddMarker("ScrollRect.value", this);
                    this.m_OnValueChanged.Invoke(this.normalizedPosition);
                    this.UpdatePrevData();
                }
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
            this.m_HasRebuiltLayout = false;
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
            }
            if (executing == CanvasUpdate.PostLayout)
            {
                this.UpdateBounds();
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

        public virtual void SetLayoutVertical()
        {
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
                this.UpdateBounds();
            }
        }

        private void SetVerticalNormalizedPosition(float value)
        {
            this.SetNormalizedPosition(value, 1);
        }

        public virtual void StopMovement()
        {
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
        
        public RectTransform content
        {
            get =>
                this.m_Content;
            set
            {
                this.m_Content = value;
            }
        }

        public virtual float flexibleHeight => -1f;

        public virtual float flexibleWidth => -1f;

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

        public virtual int layoutPriority => -1;

        public virtual float minHeight => -1f;

        public virtual float minWidth => -1f;

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

        public virtual float preferredHeight => -1f;

        public virtual float preferredWidth => -1f;

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

        [Serializable]
        public class ScrollRectEvent : UnityEvent<Vector2>
        {
        }
    }
}