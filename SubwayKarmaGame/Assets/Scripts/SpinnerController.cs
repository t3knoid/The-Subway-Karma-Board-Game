using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Animated tap-to-spin wheel that produces a random result 1–6.
///
/// Wheel segment layout — clockwise from 12 o'clock (matches spinner.jpg):
///   Seg 1 →   0°   Seg 2 →  60°   Seg 3 → 120°
///   Seg 4 → 180°   Seg 5 → 240°   Seg 6 → 300°
///
/// The fixed pointer is at 3 o'clock (90° CW from 12 o'clock).
/// Spinning the wheel clockwise lands the chosen segment on the pointer.
///
/// The spinner.jpg (2550×3300) contains both the wheel (top 70%) and the
/// pointer arrow (bottom 30%) in a single image. At runtime the two are split
/// into separate sprites; only the wheel child rotates.
///
/// Usage:
///   spinner.Spin();                      // animated tap spin
///   int val = spinner.SimulateSpin();    // instant result, no animation
///   spinner.OnSpinComplete += result => MovePlayer(result);
/// </summary>
public class SpinnerController : MonoBehaviour
{
    // ── Events ─────────────────────────────────────────────────────────────
    /// Fired when the spin animation ends. Argument: integer result 1–6.
    public event Action<int> OnSpinComplete;

    // ── State ──────────────────────────────────────────────────────────────
    public bool IsSpinning => _isSpinning;

    // ── Animation settings ─────────────────────────────────────────────────
    private const float MinDuration  = 1.5f;
    private const float MaxDuration  = 2.5f;
    private const int   MinFullSpins = 3;
    private const int   MaxFullSpins = 5;   // exclusive → 3 or 4 full rotations

    // ── Segment geometry ───────────────────────────────────────────────────
    private const float SegmentDeg = 60f;   // 360° / 6 segments
    private const float PointerDeg = 90f;   // pointer at 3 o'clock

    // ── Sprite crop — 2550×3300 spinner.jpg ──────────────────────────────
    // Wheel = top 70%,  Pointer arrow = bottom 30%.
    private const float WheelFraction = 0.70f;
    private const float PPU           = 100f;

    // ── Private fields ─────────────────────────────────────────────────────
    private bool        _isSpinning;
    private float       _totalCwDeg;       // accumulated CW degrees (never resets between spins)
    private Transform   _wheelTransform;
    private AudioSource _audio;

    // Assign via Inspector or leave null for silent spins.
    public AudioClip spinStartClip;
    public AudioClip spinStopClip;

    // ── Lifecycle ──────────────────────────────────────────────────────────
    void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        BuildSpinner();
    }

    // ── Construction ───────────────────────────────────────────────────────
    private void BuildSpinner()
    {
        Texture2D tex = Resources.Load<Texture2D>("spinner");
        if (tex == null)
        {
            Debug.LogError("[SpinnerController] Could not load 'spinner' from Resources. " +
                           "Ensure Assets/Resources/spinner.jpg exists.");
            return;
        }

        float w      = tex.width;
        float h      = tex.height;
        float arrowH = h * (1f - WheelFraction);   // height of pointer portion (px)
        float wheelH = h * WheelFraction;           // height of wheel portion (px)

        // ── Wheel child — ROTATES ──────────────────────────────────────────
        // Sprite Rect: top WheelFraction of image.
        // In Unity's y-up Rect space: y starts at arrowH (above the arrow rows).
        var wheelGO = new GameObject("SpinnerWheel");
        wheelGO.transform.SetParent(transform, false);
        _wheelTransform = wheelGO.transform;

        var wheelSR = wheelGO.AddComponent<SpriteRenderer>();
        wheelSR.sprite = Sprite.Create(
            tex,
            new Rect(0f, arrowH, w, wheelH),
            new Vector2(0.5f, 0.5f),
            PPU);
        wheelSR.sortingOrder = 10;

        // Circular collider for tap/click detection.
        // Local radius ≈ half the cropped wheel width → world radius scales with parent.
        var col = wheelGO.AddComponent<CircleCollider2D>();
        col.radius = (w * 0.45f) / PPU;   // ~45% of image width ≈ visible circle edge

        // ── Pointer child — STATIC ─────────────────────────────────────────
        // Sprite Rect: bottom (1 - WheelFraction) of image (y=0 in Unity coords).
        var ptrGO = new GameObject("SpinnerPointer");
        ptrGO.transform.SetParent(transform, false);

        var ptrSR = ptrGO.AddComponent<SpriteRenderer>();
        ptrSR.sprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, w, arrowH),
            new Vector2(0.5f, 1.0f),    // pivot at top-centre → flush below wheel
            PPU);
        ptrSR.sortingOrder = 10;

        // Position pointer flush below the wheel centre.
        float wheelWorldHalfH = (wheelH / PPU) * 0.5f;
        ptrGO.transform.localPosition = new Vector3(0f, -wheelWorldHalfH, 0f);
    }

    // ── Input ──────────────────────────────────────────────────────────────
    void OnMouseDown()
    {
        Spin();
    }

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a random integer 1–6 instantly without animation.
    /// Use for AI opponents and Express Train re-spins.
    /// </summary>
    public int SimulateSpin() => UnityEngine.Random.Range(1, 7);

    /// <summary>
    /// Starts the animated spin. Fires <see cref="OnSpinComplete"/> with the
    /// result (1–6) when the animation ends. No-op if already spinning.
    /// </summary>
    public void Spin()
    {
        if (_isSpinning) return;
        StartCoroutine(SpinCoroutine());
    }

    // ── Private ────────────────────────────────────────────────────────────
    private IEnumerator SpinCoroutine()
    {
        _isSpinning = true;

        if (spinStartClip != null) _audio.PlayOneShot(spinStartClip);

        int   result     = SimulateSpin();
        float finalCwDeg = CalculateFinalAngle(result);
        float duration   = UnityEngine.Random.Range(MinDuration, MaxDuration);
        float elapsed    = 0f;
        float startDeg   = _totalCwDeg;

        // Ease-in-out: fast middle, slow start and finish (feels like momentum + brake).
        AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / duration);
            float angle = Mathf.Lerp(startDeg, finalCwDeg, ease.Evaluate(t));
            _wheelTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
            yield return null;
        }

        _totalCwDeg = finalCwDeg;
        _wheelTransform.localEulerAngles = new Vector3(0f, 0f, -_totalCwDeg);

        if (spinStopClip != null) _audio.PlayOneShot(spinStopClip);

        _isSpinning = false;
        OnSpinComplete?.Invoke(result);

        Debug.Log($"[SpinnerController] Result: {result}");
    }

    /// <summary>
    /// Computes the cumulative CW degrees to land segment <paramref name="result"/>
    /// at the pointer (3 o'clock, 90° CW from 12 o'clock).
    /// Adds 3–4 extra full rotations for visible spin effect.
    /// </summary>
    private float CalculateFinalAngle(int result)
    {
        // After rotating CW by θ total degrees, segment N (centred at (N-1)*60°) is at:
        //   ((N-1)*60 + θ) mod 360
        // We want that to equal PointerDeg (90°):
        //   θ ≡ 90 - (N-1)*60  (mod 360)
        float segAngle       = (result - 1) * SegmentDeg;
        float targetRemainder = ((PointerDeg - segAngle) % 360f + 360f) % 360f;

        float currentMod = _totalCwDeg % 360f;
        float delta      = (targetRemainder - currentMod + 360f) % 360f;
        if (delta < 1f) delta += 360f;   // guarantee at least one visible sweep

        float extraSpins = UnityEngine.Random.Range(MinFullSpins, MaxFullSpins) * 360f;
        return _totalCwDeg + delta + extraSpins;
    }
}
