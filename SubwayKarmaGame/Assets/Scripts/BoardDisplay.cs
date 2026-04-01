using UnityEngine;

/// <summary>
/// Loads the board texture from Resources at runtime, creates a Sprite from it,
/// and scales the GameObject so the board fills the camera viewport while
/// preserving its aspect ratio.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BoardDisplay : MonoBehaviour
{
    private SpriteRenderer _renderer;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();

        Texture2D tex = Resources.Load<Texture2D>("board");
        if (tex == null)
        {
            Debug.LogError("[BoardDisplay] Could not load 'board' texture from Resources. " +
                           "Ensure Assets/Resources/board.jpg exists.");
            return;
        }

        Sprite boardSprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f);

        _renderer.sprite = boardSprite;
        FitToCamera();
    }

    // Also called in Start() to correct any stale camera state from Awake() timing.
    void Start() => FitToCamera();

    private void FitToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || _renderer.sprite == null) return;

        Vector2 spriteSize = _renderer.sprite.bounds.size;
        if (spriteSize.x <= 0 || spriteSize.y <= 0) return;

        // Derive the actual visible world bounds from the camera.
        // Using ScreenToWorldPoint is robust against any projection matrix mode
        // (physical camera gate-fit, etc.) that may adjust the effective frustum.
        Vector3 bl = cam.ScreenToWorldPoint(new Vector3(0,              0,               1f));
        Vector3 tr = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, 1f));
        float visibleWidth  = tr.x - bl.x;
        float visibleHeight = tr.y - bl.y;

        // Fit-inside: for a portrait board in a landscape camera, scaleY is always
        // the limiting dimension (board aspect 0.85 < camera aspect 1.6).
        float scale = Mathf.Min(visibleWidth / spriteSize.x, visibleHeight / spriteSize.y);

        transform.localScale = new Vector3(scale, scale, 1f);
        // Centre on the camera's actual world midpoint (handles any viewport offset).
        transform.position   = new Vector3((bl.x + tr.x) * 0.5f, (bl.y + tr.y) * 0.5f, 0f);
    }
}
