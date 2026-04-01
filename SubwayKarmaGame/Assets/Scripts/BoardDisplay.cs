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

    private void FitToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float camHeight = cam.orthographicSize * 2f;
        float camWidth  = camHeight * cam.aspect;

        Vector2 spriteSize = _renderer.sprite.bounds.size;
        if (spriteSize.x <= 0 || spriteSize.y <= 0) return;

        float scaleX = camWidth  / spriteSize.x;
        float scaleY = camHeight / spriteSize.y;
        float scale  = Mathf.Min(scaleX, scaleY); // fit-inside, preserve aspect ratio

        transform.localScale = new Vector3(scale, scale, 1f);
        transform.position   = Vector3.zero;
    }
}
