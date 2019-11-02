using UnityEngine;
using UnityEngine.EventSystems;

public class ScalableScrollRect : ScrollRectEx
{
    private Vector2 _scaleRange = new Vector2(1, 2);
    private float _speed = 0.25f;

    private int touchNum = 0;
    private Camera _cam = null;

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (Input.touchCount > 1)
        {
            return;
        }
        _cam = eventData.pressEventCamera;
        base.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (Input.touchCount > 1)
        {
            touchNum = Input.touchCount;
            return;
        }
        else if (Input.touchCount == 1 && touchNum > 1)
        {
            touchNum = Input.touchCount;
            base.OnBeginDrag(eventData);
            return;
        }
        base.OnDrag(eventData);
    }
    private float preX;
    private float preY;
    //private Vector2 center = Vector2.zero;

    private void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);

            Vector3 p1 = t1.position;
            Vector3 p2 = t2.position;

            float newX = Mathf.Abs(p1.x - p2.x);
            float newY = Mathf.Abs(p1.y - p2.y);

            //RectTransformUtility.ScreenPointToLocalPointInRectangle(content, (p1 + p2) * 0.5f, _cam, out center);
            //float x = Mathf.Clamp01((center.x - content.rect.min.x) / content.rect.width);
            //float y = Mathf.Clamp01((center.y - content.rect.min.y) / content.rect.height);
            //content.pivot = new Vector2(x, y);
            if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
            {
                preX = newX;
                preY = newY;
            }
            else if (t1.phase == TouchPhase.Moved && t2.phase == TouchPhase.Moved)
            {
                scale = (newX + newY - preX - preY) / (content.rect.width * _speed) + content.localScale.x;
                scale = Mathf.Clamp(scale, _scaleRange.x, _scaleRange.y); //限制缩放倍率
                content.localScale = new Vector3(scale, scale, scale);
            }
            preX = newX;
            preY = newY;
        }
    }
    private float scale = 0;
    //private float ratio = 0;

    protected override void LateUpdate()
    {
        if (Input.touchCount == 2)
        {
            UpdateBounds();
            Vector2 offset = CalculateOffset(Vector2.zero);
            if (offset != Vector2.zero)
            {
                Vector2 anchoredPosition = content.anchoredPosition;
                offset = CalculateOffset(anchoredPosition - content.anchoredPosition);
                anchoredPosition += offset;
                SetContentAnchoredPosition(anchoredPosition);
            }
        }
        else
        {
            base.LateUpdate();
        }
    }

    private void OnDrawGizmos()
    {
        //Gizmos.DrawSphere(center, 0.1f);
    }

    /// <summary>
    /// 限制位置
    /// </summary>
    //private void ClampPos()
    //{
    //    float half = 0.5f;
    //    float maxX = content.rect.width * scale * half - viewRect.rect.width * half;
    //    float minX = -maxX;

    //    float maxY = content.rect.height * scale * half - viewRect.rect.height * half;
    //    float minY = -maxY;

    //    Vector3 pos = content.position * ratio;
    //    //if (pos.x > maxX)
    //    //{
    //    //    pos.x = maxX;
    //    //}
    //    //else if (pos.x < minX)
    //    //{
    //    //    pos.x = minX;
    //    //}
    //    pos.x = Mathf.Clamp(pos.x, minX, maxX);
    //    //if (pos.y > maxY)
    //    //{
    //    //    pos.y = maxY;
    //    //}
    //    //else if (pos.y < minY)
    //    //{
    //    //    pos.y = minY;
    //    //}
    //    pos.y = Mathf.Clamp(pos.y, minY, maxY);
    //    pos.z = 0;
    //    content.position = pos;
    //}
}