using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScalableScrollRect2 : ScrollRect
{
    private RectTransform _contentS = null; //用于做缩放的content

    private Text _txt = null;
    private Image _imgPivot = null;

    private float _damping = 0.5f;
    private Vector2 _scaleRange = new Vector2(0.5f, 1f);
    private Dictionary<int, bool> _pointerIds = new Dictionary<int, bool>();
    private Vector2 _pos = Vector2.zero;
    private Vector2 _beginPos1 = Vector2.zero;
    private Vector2 _beginPos2 = Vector2.zero;
    private Vector2 _ingPos1 = Vector2.zero;
    private Vector2 _ingPos2 = Vector2.zero;
    private float _beginDis = 0;
    private float _ingDis = 0;

    private RectTransform _rect = null;

    private void Awake()
    {
        _rect = transform.GetComponent<RectTransform>();
        _txt = transform.parent.Find("go/Text").GetComponent<Text>();
        _contentS = content.parent.Find("ContentScale").GetComponent<RectTransform>();
        _imgPivot = _contentS.Find("imgPivot").GetComponent<Image>();
    }

    /// <summary>
    /// 初始化地图缩放参数
    /// </summary>
    /// <param name="scaleRange">Content的localScale缩放范围</param>
    /// <param name="damping">缩放速度</param>
    public void Init(Vector2 scaleRange, float damping)
    {
        _scaleRange = scaleRange;
        _damping = damping;
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {//只有第一个触控点执行，多点触控不执行
        if (Input.touchCount > 1)
        {
            return;
        }
        base.OnBeginDrag(eventData);
    }
    public override void OnDrag(PointerEventData eventData)
    {
        _pos = eventData.position;
        if (!IsPointerIdValid(eventData.pointerId))
        {
            return;
        }
        if (Input.touchCount == 1)
        {
            base.OnDrag(eventData);
            KeepTwoContentSame(content, _contentS);
        }
        else if (Input.touchCount == 2)
        {
            if (eventData.pointerId == 0)
            {
                _ingPos1.x = eventData.position.x;
                _ingPos1.y = eventData.position.y;
            }
            if (eventData.pointerId == 1)
            {
                _ingPos2.x = eventData.position.x;
                _ingPos2.y = eventData.position.y;
            }
            //设置_contentS的Pivot
            Vector2 center = (_ingPos1 + _ingPos2) / 2.0f; //两指中点屏幕坐标
            //Vector3 worldPos = Vector3.zero;
            //RectTransformUtility.ScreenPointToWorldPointInRectangle(_contentS, center, eventData.enterEventCamera, out worldPos);
            //worldPos.z = 0;
            //_imgPivot.transform.position = worldPos;
            Vector2 localPos = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_contentS, center, eventData.enterEventCamera, out localPos);
            _imgPivot.rectTransform.anchoredPosition = localPos;
            _contentS.GetLocalCorners(_coners);
            //_contentS.GetWorldCorners(_coners);
            Vector2 lb = _coners[0]; //左下角
            Vector2 rt = _coners[2]; //右上角
            //Vector2 lbWorld = _contentS.TransformPoint(lb);
            //Vector2 rtWorld = _contentS.TransformPoint(rt);
            //Vector2 disLB = localPos - lb;
            //Vector2 lbScreen = RectTransformUtility.WorldToScreenPoint(eventData.enterEventCamera, lbWorld);
            //Vector2 rtScreen = RectTransformUtility.WorldToScreenPoint(eventData.enterEventCamera, rtWorld);
            //Debug.Log($"### {lbScreen} {center} {rtScreen}");
            //_pivot.x = (center.x - lbScreen.x) * 1.0f / rtScreen.x - lbScreen.x;
            //_pivot.y = (center.y - lbScreen.y) * 1.0f / rtScreen.y - lbScreen.y;
            _pivot.x = (localPos.x - lb.x) * 1.0f / (rt.x - lb.x);
            _pivot.y = (localPos.y - lb.y) * 1.0f / (rt.y - lb.y);
            Debug.Log($"### {lb} {localPos} {rt}");
            _contentS.pivot = _pivot; //相对坐标转换为0-1
            //
            _ingDis = Vector2.Distance(_ingPos1, _ingPos2);
            float scale = _ingDis * 1.0f / _beginDis; //缩放倍率
            scale = Mathf.Clamp(scale, _scaleRange.x, _scaleRange.y); //限制缩放倍率
            _contentScale.x = scale;
            _contentScale.y = scale;
            _contentS.localScale = _contentScale;
            //
            KeepTwoContentSame(_contentS, content);
        }
    }
    public override void OnEndDrag(PointerEventData eventData)
    {
        _pointerIds[eventData.pointerId] = false;
        if (!IsPointerIdValid(eventData.pointerId))
        {
            return;
        }
        if (Input.touchCount == 1)
        {
            base.OnEndDrag(eventData);
        }
        else if (Input.touchCount == 2)
        {
            if (eventData.pointerId == 0)
            {

            }
            if (eventData.pointerId == 1)
            {

            }
        }
    }
    private bool IsPointerIdValid(int pointerId)
    {
        return pointerId == 0 || pointerId == 1;
    }
    //保持c2和c1一致
    private void KeepTwoContentSame(RectTransform c1, RectTransform c2)
    {
        c2.anchoredPosition = c1.anchoredPosition;
        c2.pivot = c1.pivot;
        c2.localScale = c1.localScale;
    }

    /// <summary>
    /// 计算出content位于四个边界情况的pivot范围
    /// </summary>
    /// <returns></returns>
    private Rect ContentPosScope()
    {
        return Rect.zero;
    }

    private void Update()
    {
        sb.Clear();
        foreach (var item in _pointerIds)
        {
            if(item.Value)
            {
                sb.Append(item.Key);
            }
        }
        _txt.text = $"{sb}\n{_pos}\n{_pivot}\n{_contentScale}";
    }
    StringBuilder sb = new StringBuilder();
    //private int _touchNum = 0;
    //private Vector2 _prev = Vector2.zero;
    //private Vector2 _dis = Vector2.zero;
    private Vector2 _contentScale = Vector2.zero;
    private float _minX = 0;
    private float _maxX = 0;
    private float _minY = 0;
    private float _maxY = 0;
    private Vector2 _pivot = Vector2.zero;
    //private float _scale = 0;
    //private float _ratio = 0;
    //private Vector3 _pos = Vector3.zero;
    private Vector3[] _coners = new Vector3[4];
}