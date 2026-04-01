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

    // Also called in Start() to correct any stale camera aspect from Awake() timing.
    void Start() => FitToCamera();

    private void FitToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector2 spriteSize = _renderer.sprite != null ? _renderer.sprite.bounds.size : Vector2.zero;
        if (spriteSize.x <= 0 || spriteSize.y <= 0) return;

        // Scale so the board fills the camera height exactly.
        // For our landscape target (aspect > board aspect 0.85) this is always the
        // smaller scale (fit-inside), so nothing overflows horizontally.
        float camHeight = cam.orthographicSize * 2f;
        float scale     = camHeight / spriteSize.y;

        transform.localScale = new Vector3(scale, scale, 1f);
        transform.position   = Vector3.zero;
    }
}
