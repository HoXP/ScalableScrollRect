using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScalableScrollRect1 : ScrollRect
{
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
        _imgPivot = content.Find("imgPivot").GetComponent<Image>();
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
    {
        _pointerIds[eventData.pointerId] = true;
        if (!IsPointerIdValid(eventData.pointerId))
        {
            return;
        }
        if (Input.touchCount == 1)
        {
            base.OnBeginDrag(eventData);
        }
        else if (Input.touchCount == 2)
        {
            if (eventData.pointerId == 0)
            {
                _beginPos1.x = eventData.position.x;
                _beginPos1.y = eventData.position.y;
            }
            if (eventData.pointerId == 1)
            {
                _beginPos2.x = eventData.position.x;
                _beginPos2.y = eventData.position.y;
            }
            _beginDis = Vector2.Distance(_beginPos1, _beginPos2);
        }
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
            //设置content的Pivot
            Vector2 center = (_ingPos1 + _ingPos2) / 2.0f; //两指中点屏幕坐标
            //Vector3 worldPos = Vector3.zero;
            //RectTransformUtility.ScreenPointToWorldPointInRectangle(content, center, eventData.enterEventCamera, out worldPos);
            //worldPos.z = 0;
            //_imgPivot.transform.position = worldPos;
            Vector2 localPos = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, center, eventData.enterEventCamera, out localPos);
            _imgPivot.rectTransform.anchoredPosition = localPos;
            Vector3[] coners = null;
            content.GetLocalCorners(coners);
            Vector2 lb = coners[1]; //左下角
            Vector2 disLB = localPos - lb;
            Vector2 pivot = Vector2.zero;
            pivot.x = disLB.x / _rect.rect.width;
            pivot.x = disLB.y / _rect.rect.height;
            content.pivot = pivot; //相对坐标转换为0-1
            //
            _ingDis = Vector2.Distance(_ingPos1, _ingPos2);
            float scale = _ingDis / _beginDis; //缩放倍率
            scale = Mathf.Clamp(scale, _scaleRange.x, _scaleRange.y); //限制缩放倍率
            _contentScale.x = scale;
            _contentScale.y = scale;
            content.localScale = _contentScale;
            //限制位置
            _maxX = content.rect.width * scale / 2 - viewRect.rect.width / 2;
            if (_maxX < 0) _maxX = 0;
            _minX = -_maxX;
            _maxY = content.rect.height * scale / 2 - viewRect.rect.height / 2;
            if (_maxY < 0) _maxY = 0;
            _minY = -_maxY;
            _pos.x = Mathf.Clamp(_pos.x, _minX, _maxX);
            _pos.y = Mathf.Clamp(_pos.y, _minY, _maxY);
            content.position = _pos;
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
        _txt.text = $"{sb}\n{_pos}";
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
    //private float _scale = 0;
    //private float _ratio = 0;
    //private Vector3 _pos = Vector3.zero;
}