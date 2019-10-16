using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScalableScrollRect : ScrollRect
{
    private float _damping = 0.5f;
    private Vector2 _scaleRange = new Vector2(0.5f, 1f);

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
        if (Input.touchCount > 1)
        {
            return;
        }
        base.OnBeginDrag(eventData);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (Input.touchCount > 1)
        {
            _touchNum = Input.touchCount;
            return;
        }
        else if (Input.touchCount == 1 && _touchNum > 1)
        {
            _touchNum = Input.touchCount;
            base.OnBeginDrag(eventData);
            return;
        }
        base.OnDrag(eventData);
    }

    private void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);

            _dis.x = Mathf.Abs(t1.position.x - t2.position.x);
            _dis.y = Mathf.Abs(t1.position.y - t2.position.y);

            if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
            {
                _prev.x = _dis.x;
                _prev.y = _dis.y;
            }
            else if (t1.phase == TouchPhase.Moved && t2.phase == TouchPhase.Moved)
            {
                _scale = (_dis.x + _dis.y - _prev.x - _prev.y) / (content.rect.width * _damping) + content.localScale.x;
                //_scale = (_dis.x - _prev.x) / (content.rect.width * _damping) + content.localScale.x;

                if (_scale > _scaleRange.x && _scale < _scaleRange.y)
                {
                    _ratio = _scale / content.localScale.x;
                    _contentScale.x = _scale;
                    _contentScale.y = _scale;
                    content.localScale = _contentScale;

                    _maxX = content.rect.width * _scale / 2 - viewRect.rect.width / 2;
                    if (_maxX < 0) _maxX = 0;
                    _minX = -_maxX;
                    _maxY = content.rect.height * _scale / 2 - viewRect.rect.height / 2;
                    if (_maxY < 0) _maxY = 0;
                    _minY = -_maxY;

                    _pos = content.position * _ratio;
                    _pos.x = Mathf.Clamp(_pos.x, _minX, _maxX);
                    _pos.y = Mathf.Clamp(_pos.y, _minY, _maxY);
                    content.position = _pos;
                }
            }
            _prev.x = _dis.x;
            _prev.y = _dis.y;
        }
    }
    private int _touchNum = 0;
    private Vector2 _prev = Vector2.zero;
    private Vector2 _dis = Vector2.zero;
    private Vector2 _contentScale = Vector2.zero;
    private float _minX = 0;
    private float _maxX = 0;
    private float _minY = 0;
    private float _maxY = 0;
    private float _scale = 0;
    private float _ratio = 0;
    private Vector3 _pos = Vector3.zero;
}