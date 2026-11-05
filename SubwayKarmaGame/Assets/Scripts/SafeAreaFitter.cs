using UnityEngine;

/// <summary>
/// Keeps a UI RectTransform inside Screen.safeArea, the part of the display not
/// covered by camera notches, punch-hole cutouts, rounded corners or system bars.
/// Re-applies whenever the safe area changes (rotation, resize, split-screen).
///
/// Usage: put HUD elements under SafeAreaFitter.Create(canvasTransform) instead
/// of directly under the canvas. Full-screen backdrops should stay on the canvas.
///
/// Also provides WorldRect() so world-space objects (e.g. the spinner) can stay
/// inside the safe area.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rect;
    private Rect          _appliedSafeArea;
    private Vector2Int    _appliedScreen;

    /// <summary>Creates a full-canvas child that is shrunk to the safe area.</summary>
    public static RectTransform Create(Transform canvas, string name = "SafeArea")
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(canvas, false);
        go.AddComponent<SafeAreaFitter>();   // Awake applies immediately
        return (RectTransform)go.transform;
    }

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != _appliedSafeArea ||
            Screen.width  != _appliedScreen.x ||
            Screen.height != _appliedScreen.y)
            Apply();
    }

    private void Apply()
    {
        _appliedSafeArea = Screen.safeArea;
        _appliedScreen   = new Vector2Int(Screen.width, Screen.height);
        if (Screen.width <= 0 || Screen.height <= 0) return;

        Vector2 min = _appliedSafeArea.min;
        Vector2 max = _appliedSafeArea.max;
        min.x /= Screen.width;  min.y /= Screen.height;
        max.x /= Screen.width;  max.y /= Screen.height;

        _rect.anchorMin = min;
        _rect.anchorMax = max;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }

    /// <summary>The safe area in world units for an orthographic camera.</summary>
    public static Rect WorldRect(Camera cam)
    {
        Rect sa = Screen.safeArea;
        if (cam == null || sa.width <= 0f || sa.height <= 0f)
            return new Rect();

        Vector3 lo = cam.ScreenToWorldPoint(new Vector3(sa.xMin, sa.yMin, 0f));
        Vector3 hi = cam.ScreenToWorldPoint(new Vector3(sa.xMax, sa.yMax, 0f));
        return Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
    }
}
