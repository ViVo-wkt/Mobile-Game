using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CustomJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("UI Elements")]
    public Image background;
    public Image handle;

    [Header("Settings")]
    public float handleRange = 80f;
    public bool snapBack = true;

    [Header("Output")]
    public Vector2 Direction { get; private set; } = Vector2.zero;
    public float Magnitude => Direction.magnitude;

    private Vector2 _startPos;
    private Canvas _canvas;
    private RectTransform _bgRect;

    void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _bgRect = background.rectTransform;
        _startPos = handle.rectTransform.anchoredPosition;
        handle.enabled = false;
    }

    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_bgRect, eventData.position, _canvas.worldCamera, out localPos);

        Vector2 offset = localPos;
        float radius = handleRange;

        if (offset.magnitude > radius)
            offset = offset.normalized * radius;

        handle.rectTransform.anchoredPosition = _startPos + offset;
        Direction = offset / radius;
        handle.enabled = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Direction = Vector2.zero;
        handle.rectTransform.anchoredPosition = _startPos;
        handle.enabled = !snapBack;
    }
}