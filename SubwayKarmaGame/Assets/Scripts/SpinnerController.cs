using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Animated tap-to-spin arrow that produces a random result 1-6,
/// matching the physical board-game spinner.
///
/// The physical spinner has:
///   - A static numbered wheel (spinner.png, 2129x2107, transparent bg).
///   - A rotating arrow (spinner_arrow.png, 948x452, transparent bg).
///     Players flick the arrow; it spins and stops on a number.
///
/// Segment layout clockwise from 12 o'clock:
///   Seg 1 ->   0 deg   Seg 2 ->  60 deg   Seg 3 -> 120 deg
///   Seg 4 -> 180 deg   Seg 5 -> 240 deg   Seg 6 -> 300 deg
///
/// Image measurements:
///   spinner.png  2129x2107 — pivot dot at (1062, 1062) image top-down
///     Unity pivot fraction: (1062/2129, 1045/2107) = (0.4989, 0.4960) ~ centre
///   spinner_arrow.png  948x452 — pivot dot at (85, 230) image top-down
///     Unity pivot fraction: (85/948, 222/452) = (0.0897, 0.4912)
///     Pivot near left edge; arrow extends rightward — matches Unity 0-deg = right.
///
/// Usage:
///   spinner.Spin();                      // animated tap spin
///   int val = spinner.SimulateSpin();    // instant result, no animation
///   spinner.OnSpinComplete += result => MovePlayer(result);
/// </summary>
public class SpinnerController : MonoBehaviour
{
    // -- Events -------------------------------------------------------------
    /// Fired when the spin animation ends. Argument: integer result 1-6.
    public event Action<int> OnSpinComplete;

    // -- State --------------------------------------------------------------
    public bool IsSpinning => _isSpinning;

    // -- Animation settings -------------------------------------------------
    private const float MinDuration  = 1.5f;
    private const float MaxDuration  = 2.5f;
    private const int   MinFullSpins = 3;
    private const int   MaxFullSpins = 5;   // exclusive: 3 or 4 full rotations

    // -- Geometry -----------------------------------------------------------
    private const float SegmentDeg     = 60f;  // 360 / 6 segments
    // Arrow at 0 deg Unity Z rotation points RIGHT (+X), which is 90 deg CW from north.
    // To point at segment N (centred at (N-1)*60 deg CW from north), arrow must
    // have rotated CW by:   theta = (N-1)*60 - 90   (mod 360)
    private const float ArrowInitialDeg = 90f;

    // -- Image measurements -------------------------------------------------
    private const float PPU = 100f;

    // spinner.png 2129x2107 — pivot dot at image pixel (1062, 1062)
    // Unity Y is bottom-up: pivotY = (2107-1062)/2107
    private const float WheelPivotX = 1062f / 2129f;  // 0.4989
    private const float WheelPivotY = 1045f / 2107f;  // 0.4960

    // spinner_arrow.png 948x452 — pivot dot at image pixel (85, 230)
    // Unity Y is bottom-up: pivotY = (452-230)/452
    private const float ArrowPivotX = 85f  / 948f;   // 0.0897
    private const float ArrowPivotY = 222f / 452f;   // 0.4912

    // Spinner wheel fills this fraction of camera height.
    private const float ViewHeightFraction = 0.70f;

    // -- Private fields -----------------------------------------------------
    private bool        _isSpinning;
    private float       _totalCwDeg;    // accumulated CW rotation of the arrow
    private Transform   _arrowTransform;
    private AudioSource _audio;

    // Assign in Inspector or leave null for silent spins.
    public AudioClip spinStartClip;
    public AudioClip spinStopClip;

    // -- Lifecycle ----------------------------------------------------------
    void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
    }

    void Start()
    {
        BuildSpinner();
    }

    // -- Construction -------------------------------------------------------
    private void BuildSpinner()
    {
        Texture2D wheelTex = Resources.Load<Texture2D>("spinner");
        Texture2D arrowTex = Resources.Load<Texture2D>("spinner_arrow");

        if (wheelTex == null)
        {
            Debug.LogError("[SpinnerController] Could not load 'spinner' from Resources. " +
                           "Ensure Assets/Resources/spinner.png exists.");
            return;
        }
        if (arrowTex == null)
        {
            Debug.LogError("[SpinnerController] Could not load 'spinner_arrow' from Resources. " +
                           "Ensure Assets/Resources/spinner_arrow.png exists.");
            return;
        }

        float wW = wheelTex.width;
        float wH = wheelTex.height;

        // Scale parent so the wheel fills ViewHeightFraction of camera height.
        Camera cam  = Camera.main;
        float camH  = cam != null ? cam.orthographicSize * 2f : 10f;
        float scale = (camH * ViewHeightFraction) / (wH / PPU);
        transform.localScale = new Vector3(scale, scale, 1f);
        transform.position   = cam != null
            ? new Vector3(cam.transform.position.x, cam.transform.position.y, 0f)
            : Vector3.zero;

        // -- Wheel child: STATIC --------------------------------------------
        var wheelGO = new GameObject("SpinnerWheel");
        wheelGO.transform.SetParent(transform, false);
        wheelGO.transform.localPosition = Vector3.zero;

        var wheelSR = wheelGO.AddComponent<SpriteRenderer>();
        wheelSR.sprite = Sprite.Create(
            wheelTex,
            new Rect(0f, 0f, wW, wH),
            new Vector2(WheelPivotX, WheelPivotY),
            PPU);
        wheelSR.sortingOrder = 10;

        // -- Circle collider on the PARENT so OnMouseDown fires here --------
        var col = gameObject.AddComponent<CircleCollider2D>();
        col.radius = (Mathf.Min(wW, wH) * 0.45f) / PPU;

        // -- Arrow child: ROTATES -------------------------------------------
        // Pivot (ArrowPivotX, ArrowPivotY) places the dot at local (0,0),
        // which is the wheel centre — so the arrow orbits the correct point.
        var arrowGO = new GameObject("SpinnerArrow");
        arrowGO.transform.SetParent(transform, false);
        arrowGO.transform.localPosition = Vector3.zero;
        _arrowTransform = arrowGO.transform;

        float aW = arrowTex.width;
        float aH = arrowTex.height;
        var arrowSR = arrowGO.AddComponent<SpriteRenderer>();
        arrowSR.sprite = Sprite.Create(
            arrowTex,
            new Rect(0f, 0f, aW, aH),
            new Vector2(ArrowPivotX, ArrowPivotY),
            PPU);
        arrowSR.sortingOrder = 11;
    }

    // -- Input --------------------------------------------------------------
    void OnMouseDown()
    {
        Spin();
    }

    // -- Public API ---------------------------------------------------------

    /// <summary>Returns a random integer 1-6 instantly (no animation).</summary>
    public int SimulateSpin() => UnityEngine.Random.Range(1, 7);

    /// <summary>
    /// Starts the animated spin. Fires <see cref="OnSpinComplete"/> with
    /// the result (1-6) when the animation ends. No-op if already spinning.
    /// </summary>
    public void Spin()
    {
        if (_isSpinning) return;
        StartCoroutine(SpinCoroutine());
    }

    // -- Private ------------------------------------------------------------
    private IEnumerator SpinCoroutine()
    {
        _isSpinning = true;

        if (spinStartClip != null) _audio.PlayOneShot(spinStartClip);

        int   result     = SimulateSpin();
        float finalCwDeg = CalculateFinalAngle(result);
        float duration   = UnityEngine.Random.Range(MinDuration, MaxDuration);
        float elapsed    = 0f;
        float startDeg   = _totalCwDeg;

        AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / duration);
            float angle = Mathf.Lerp(startDeg, finalCwDeg, ease.Evaluate(t));
            _arrowTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
            yield return null;
        }

        _totalCwDeg = finalCwDeg;
        _arrowTransform.localEulerAngles = new Vector3(0f, 0f, -_totalCwDeg);

        if (spinStopClip != null) _audio.PlayOneShot(spinStopClip);

        _isSpinning = false;
        OnSpinComplete?.Invoke(result);

        Debug.Log($"[SpinnerController] Result: {result}");
    }

    /// <summary>
    /// Computes cumulative CW degrees so the arrow lands on segment
    /// <paramref name="result"/> (1-6) plus 3-4 extra full spins.
    ///
    /// Arrow at 0 deg points right (90 deg CW from 12 o'clock).
    /// After rotating CW by theta deg the arrow points at (90+theta) mod 360
    /// degrees CW from north. We want that to equal (result-1)*60 deg.
    ///   theta = (result-1)*60 - 90   (mod 360)
    /// </summary>
    private float CalculateFinalAngle(int result)
    {
        float segAngle        = (result - 1) * SegmentDeg;
        float targetRemainder = ((segAngle - ArrowInitialDeg) % 360f + 360f) % 360f;

        float currentMod = _totalCwDeg % 360f;
        float delta      = (targetRemainder - currentMod + 360f) % 360f;
        if (delta < 1f) delta += 360f;   // guarantee at least one visible sweep

        float extraSpins = UnityEngine.Random.Range(MinFullSpins, MaxFullSpins) * 360f;
        return _totalCwDeg + delta + extraSpins;
    }
}
