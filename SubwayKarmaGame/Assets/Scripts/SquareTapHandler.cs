using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to a GameObject with a Collider2D on each perimeter square.
/// Handles both mouse click (WebGL desktop) and touch (mobile browser).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SquareTapHandler : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Index of this square in the 20-square board loop (0–19).")]
    public int squareIndex;

    // Fired when this square is tapped/clicked. Parameter is the square index.
    public static UnityEvent<int> OnAnySquareTapped = new UnityEvent<int>();

    private BoardSquare _square;
    private SpriteRenderer _highlightRenderer;
    private Color _originalColor;

    void Start()
    {
        if (BoardManager.Instance != null)
            _square = BoardManager.Instance.GetSquare(squareIndex);

        _highlightRenderer = GetComponent<SpriteRenderer>();
        if (_highlightRenderer != null)
            _originalColor = _highlightRenderer.color;
    }

    // Called by Unity's EventSystem for mouse click and touch tap.
    public void OnPointerClick(PointerEventData eventData)
    {
        HandleTap();
    }

    // Fallback for non-EventSystem environments (e.g. Physics2D.Raycast approach).
    void OnMouseDown()
    {
        HandleTap();
    }

    private void HandleTap()
    {
        if (_square == null) return;

        Debug.Log($"[SquareTap] index={_square.squareIndex} type={_square.type} name=\"{_square.displayName}\"");

        FlashSquare();

        OnAnySquareTapped.Invoke(squareIndex);

        if (BoardManager.Instance != null)
            BoardManager.Instance.OnPlayerLands.Invoke(squareIndex);
    }

    private void FlashSquare()
    {
        if (_highlightRenderer == null) return;

        Color flashColor = _square.polarity == SquarePolarity.Negative
            ? new Color(1f, 0.2f, 0.2f, 0.6f)    // red flash for negative
            : new Color(0.2f, 1f, 0.2f, 0.6f);    // green flash for positive/neutral

        StopAllCoroutines();
        StartCoroutine(FlashCoroutine(flashColor));
    }

    private System.Collections.IEnumerator FlashCoroutine(Color flashColor)
    {
        if (_highlightRenderer == null) yield break;

        _highlightRenderer.color = flashColor;
        yield return new WaitForSeconds(0.35f);
        _highlightRenderer.color = _originalColor;
    }
}
