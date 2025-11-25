// File: JoystickAnchor.cs
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class JoystickAnchor : MonoBehaviour
{
    public enum AnchorPreset
    {
        BottomLeft, BottomRight, TopLeft, TopRight, Center
    }

    [Header("Position")]
    public AnchorPreset preset = AnchorPreset.BottomLeft;
    public Vector2 offset = Vector2.zero;
    public Vector2 size = new Vector2(200, 200);        // background size

    [Header("Visuals")]
    public Color backgroundColor = new Color(1, 1, 1, 0.3f);
    public Color handleColor = Color.white;

    private void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying) Apply();
#endif
    }

    public void Apply()
    {
        RectTransform rt = (RectTransform)transform;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (!canvas) return;

        rt.sizeDelta = size;

        switch (preset)
        {
            case AnchorPreset.BottomLeft:
                rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0, 0);
                break;
            case AnchorPreset.BottomRight:
                rt.anchorMin = rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                break;
            case AnchorPreset.TopLeft:
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                break;
            case AnchorPreset.TopRight:
                rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                break;
            case AnchorPreset.Center:
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                break;
        }

        rt.anchoredPosition = offset;

        Image bg = GetComponent<Image>();
        if (bg) bg.color = backgroundColor;

        Image handle = transform.GetComponentInChildren<Image>(true);
        if (handle && handle != bg) handle.color = handleColor;
    }
}