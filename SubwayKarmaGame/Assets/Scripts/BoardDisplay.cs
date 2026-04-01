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
        // Do NOT call FitToCamera here — cam.aspect is unreliable at Awake() time.
        // Start() is guaranteed to run after the first frame, when the camera is ready.
    }

    void Start() => FitToCamera();

    private void FitToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null || _renderer.sprite == null) return;

        Vector2 spriteSize = _renderer.sprite.bounds.size;
        if (spriteSize.x <= 0 || spriteSize.y <= 0) return;

        // The scene camera uses Automatic orthographic mode (no physical gate-fit).
        // cam.orthographicSize and cam.aspect are the exact canonical values.
        float camHeight = cam.orthographicSize * 2f;               // 10 world units
        float camWidth  = camHeight * cam.aspect;                  //  16 world units @ 960×600

        // Fit-inside: portrait board (aspect 0.85) in landscape camera (aspect 1.6).
        // scaleY (0.353) < scaleX (0.663) so scaleY always wins — fills the height.
        float scale = Mathf.Min(camWidth / spriteSize.x, camHeight / spriteSize.y);

        transform.localScale = new Vector3(scale, scale, 1f);
        transform.position   = new Vector3(cam.transform.position.x,
                                           cam.transform.position.y, 0f);
    }
}
