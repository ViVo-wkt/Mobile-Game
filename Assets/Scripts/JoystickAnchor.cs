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
    public Vector2 offset = Vector2.zero;               // extra pixels from the edge
    public Vector2 size = new Vector2(200, 200);        // background size (optional)

    [Header("Visuals")]
    public Color backgroundColor = new Color(1, 1, 1, 0.3f);
    public Color handleColor = Color.white;

    private void Update()
    {
        // Only runs in edit-mode so you see the result instantly
#if UNITY_EDITOR
        if (!Application.isPlaying) Apply();
#endif
    }

    public void Apply()
    {
        RectTransform rt = (RectTransform)transform;
        Canvas canvas = GetComponentInParent<Canvas>();
        if (!canvas) return;

        // ---- 1. Set size ----
        rt.sizeDelta = size;

        // ---- 2. Set anchor + pivot ----
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

        // ---- 3. Apply offset ----
        rt.anchoredPosition = offset;

        // ---- 4. Apply colours (optional) ----
        Image bg = GetComponent<Image>();
        if (bg) bg.color = backgroundColor;

        Image handle = transform.GetComponentInChildren<Image>(true);
        if (handle && handle != bg) handle.color = handleColor;
    }
}