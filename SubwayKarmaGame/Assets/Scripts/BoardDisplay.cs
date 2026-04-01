using UnityEngine;

/// <summary>
/// Loads the board sprite from Resources at runtime and scales it so that it
/// fits entirely within the camera's orthographic view while preserving its
/// aspect ratio.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BoardDisplay : MonoBehaviour
{
    private SpriteRenderer _renderer;

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();

        Sprite boardSprite = Resources.Load<Sprite>("board");
        if (boardSprite == null)
        {
            Debug.LogError("[BoardDisplay] Could not load 'board' sprite from Resources. " +
                           "Ensure Assets/Resources/board.jpg exists and is imported as Sprite.");
            return;
        }

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
