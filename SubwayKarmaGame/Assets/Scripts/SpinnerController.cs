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
/// Player gestures:
///   Tap (release)       → spin
///   Drag                → "lift" it (grows, casts a shadow, floats above tokens) and move it
///                         anywhere on screen; dropping it saves the spot in PlayerPrefs
///   Press and hold ~1 s → return it to automatic placement
///
/// Usage:
///   spinner.Spin();                      // animated tap spin
///   int val = spinner.SimulateSpin();    // instant result, no animation
///   spinner.OnSpinComplete += result => MovePlayer(result);
/// </summary>
public class SpinnerController : MonoBehaviour
{
    // -- Singleton ----------------------------------------------------------
    public static SpinnerController Instance { get; private set; }

    // -- Events -------------------------------------------------------------
    /// Fired when the spin animation ends. Argument: integer result 1-6.
    public event Action<int> OnSpinComplete;

    // -- State --------------------------------------------------------------
    public bool IsSpinning => _isSpinning;

    /// When false, taps are ignored (TurnController locks it outside a human's spin).
    public bool Interactable { get; set; } = true;

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

    // -- Layout -------------------------------------------------------------
    // Preferred: in the empty strip LEFT of the board (the score panel is top-right).
    private const float SideMarginFill     = 0.85f;  // wheel diameter as a fraction of the strip width
    private const float SideMaxHeightFrac  = 0.45f;  // never taller than this fraction of the screen
    private const float SideMinHeightFrac  = 0.22f;  // smaller than this → use the board centre instead

    // Fallback (portrait / 4:3): inside the board's centre panel, clear of the squares.
    // Inner panel size from BoardSetup measurements: 1 − 2×391/2411 and 1 − 2×392/2835.
    private const float BoardInnerWidthFrac  = 0.676f;
    private const float BoardInnerHeightFrac = 0.723f;
    private const float CentreFill           = 0.60f;

    // -- Private fields -----------------------------------------------------
    private float          _wheelWorldHeight;   // wheel height in world units at scale 1
    private SpriteRenderer _boardRenderer;
    private float          _lastAspect;
    private Vector3        _lastBoardSize;
    private Rect           _lastSafeArea;

    // -- Player-positioned spinner -----------------------------------------
    // Drag to move; position is saved as a viewport fraction so it survives
    // resizes/rotation. Press-and-hold without moving restores auto placement.
    private const string PrefX              = "spinner.viewportX";
    private const string PrefY              = "spinner.viewportY";
    private const float  DragThresholdInch  = 0.06f;  // movement before a press counts as a drag
    private const float  LongPressSeconds   = 1.0f;

    private bool    _hasCustomPosition;
    private Vector2 _customViewport;

    private bool    _pointerDown;
    private bool    _dragging;
    private bool    _longPressHandled;
    private float   _pressTime;
    private Vector3 _pressScreenPos;
    private Vector3 _grabOffset;

    private bool        _isSpinning;
    private float       _totalCwDeg;    // accumulated CW rotation of the arrow
    private Transform   _arrowTransform;

    // -- "Lifted" look while dragging --------------------------------------
    private const float LiftScale        = 1.15f;            // grows 15% when picked up
    private const float LiftSpeed        = 12f;              // ease rate (higher = snappier)
    private const float LiftedZ          = -1f;              // in front of the tokens (z = -0.5)
    private const float ShadowAlpha      = 0.35f;
    private static readonly Vector2 ShadowOffset = new Vector2(0.8f, -1.2f);  // wheel-local units (~4–6% of the wheel) at full lift

    private float          _baseScale = 1f;   // size chosen by Layout()
    private float          _lift      = 1f;   // current multiplier, eases toward the target
    private SpriteRenderer _shadow;
    private AudioSource _audio;

    // Assign in Inspector or leave null for silent spins.
    public AudioClip spinStartClip;
    public AudioClip spinStopClip;

    // -- Lifecycle ----------------------------------------------------------
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        LoadCustomPosition();
    }

    void Start()
    {
        BuildSpinner();
        Layout();
    }

    void OnEnable()
    {
        // The spinner is hidden between human turns; re-check placement when shown.
        if (_wheelWorldHeight > 0f) Layout();
    }

    void LateUpdate()
    {
        // Re-layout on window resize / device rotation, or once the board has been fitted.
        Camera cam = Camera.main;
        if (cam == null || _wheelWorldHeight <= 0f || _dragging) return;
        if (!Mathf.Approximately(cam.aspect, _lastAspect) ||
            BoardBounds(cam).size != _lastBoardSize ||
            Screen.safeArea != _lastSafeArea)
            Layout();
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

        // Size and position are set by Layout(), which also runs on resize/rotation.
        _wheelWorldHeight = wH / PPU;

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

        // -- Drop shadow: hidden until the spinner is picked up -------------
        var shadowGO = new GameObject("SpinnerShadow");
        shadowGO.transform.SetParent(transform, false);
        _shadow = shadowGO.AddComponent<SpriteRenderer>();
        _shadow.sprite       = wheelSR.sprite;
        _shadow.color        = new Color(0f, 0f, 0f, 0f);
        _shadow.sortingOrder = 9;   // just under the wheel

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

    // -- Layout -------------------------------------------------------------

    /// <summary>
    /// Places the spinner so the board's squares stay visible:
    ///   • Landscape: centred in the empty strip left of the board.
    ///   • Too narrow for that (portrait, 4:3): shrunk into the board's centre panel.
    /// </summary>
    private void Layout()
    {
        Camera cam = Camera.main;
        if (cam == null || _wheelWorldHeight <= 0f) return;

        float camH = cam.orthographicSize * 2f;
        Vector3 camPos = cam.transform.position;
        Bounds board = BoardBounds(cam);

        // Measure the left strip from the safe-area edge, not the physical screen
        // edge, so a landscape camera cutout on the left can't cover the spinner.
        Rect  safe       = SafeWorldRect(cam);
        float screenLeft = safe.xMin;
        float sideStrip  = board.min.x - screenLeft;
        float sideSize   = Mathf.Min(sideStrip * SideMarginFill, camH * SideMaxHeightFrac);

        float   diameter;
        Vector3 centre;
        if (sideSize >= camH * SideMinHeightFrac)
        {
            diameter = sideSize;
            centre   = new Vector3(screenLeft + sideStrip * 0.5f, camPos.y, 0f);
        }
        else
        {
            diameter = Mathf.Min(board.size.x * BoardInnerWidthFrac,
                                 board.size.y * BoardInnerHeightFrac) * CentreFill;
            centre   = new Vector3(board.center.x, board.center.y, 0f);
        }

        // A spot the player dragged it to wins over the automatic position (size stays automatic).
        if (_hasCustomPosition)
        {
            Vector3 p = cam.ViewportToWorldPoint(new Vector3(_customViewport.x, _customViewport.y, 0f));
            centre = new Vector3(p.x, p.y, 0f);
        }

        float scale = diameter / _wheelWorldHeight;
        _baseScale = scale;
        ApplyLift();
        transform.position = ClampToScreen(cam, centre);

        _lastAspect    = cam.aspect;
        _lastBoardSize = board.size;
        _lastSafeArea  = Screen.safeArea;
    }

    /// <summary>World bounds of the board sprite, or an estimate if it isn't ready yet.</summary>
    private Bounds BoardBounds(Camera cam)
    {
        if (_boardRenderer == null)
        {
            var display = FindAnyObjectByType<BoardDisplay>();
            if (display != null) _boardRenderer = display.GetComponent<SpriteRenderer>();
        }
        if (_boardRenderer != null && _boardRenderer.sprite != null)
            return _boardRenderer.bounds;

        // Board image is 2411×2835 and fills the screen height.
        float camH = cam.orthographicSize * 2f;
        Vector3 c = cam.transform.position;
        return new Bounds(new Vector3(c.x, c.y, 0f), new Vector3(camH * 2411f / 2835f, camH, 0f));
    }

    // -- Lift effect ----------------------------------------------------------

    void Update()
    {
        float target = _dragging ? LiftScale : 1f;
        if (Mathf.Approximately(_lift, target)) return;

        _lift = Mathf.MoveTowards(_lift, target, LiftSpeed * (LiftScale - 1f) * Time.unscaledDeltaTime);
        ApplyLift();
    }

    /// <summary>Applies size, shadow and depth for the current lift amount.</summary>
    private void ApplyLift()
    {
        float s = _baseScale * _lift;
        transform.localScale = new Vector3(s, s, 1f);

        float t = Mathf.InverseLerp(1f, LiftScale, _lift);   // 0 = resting, 1 = fully lifted

        Vector3 p = transform.position;
        transform.position = new Vector3(p.x, p.y, Mathf.Lerp(0f, LiftedZ, t));

        if (_shadow != null)
        {
            _shadow.transform.localPosition = new Vector3(ShadowOffset.x * t, ShadowOffset.y * t, 0.01f);
            _shadow.color = new Color(0f, 0f, 0f, ShadowAlpha * t);
        }
    }

    /// <summary>Keeps the whole wheel on screen.</summary>
    private Vector3 ClampToScreen(Camera cam, Vector3 pos)
    {
        float radius = _wheelWorldHeight * _baseScale * _lift * 0.5f;
        Rect  safe   = SafeWorldRect(cam);   // stay clear of notches and rounded corners

        float x = safe.width  > radius * 2f ? Mathf.Clamp(pos.x, safe.xMin + radius, safe.xMax - radius) : safe.center.x;
        float y = safe.height > radius * 2f ? Mathf.Clamp(pos.y, safe.yMin + radius, safe.yMax - radius) : safe.center.y;
        return new Vector3(x, y, transform.position.z);   // keep the lift depth
    }

    /// <summary>Safe area in world units, falling back to the full view if unavailable.</summary>
    private static Rect SafeWorldRect(Camera cam)
    {
        Rect safe = SafeAreaFitter.WorldRect(cam);
        if (safe.width > 0f && safe.height > 0f) return safe;

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 c   = cam.transform.position;
        return Rect.MinMaxRect(c.x - halfW, c.y - halfH, c.x + halfW, c.y + halfH);
    }

    // -- Saved position -----------------------------------------------------

    private void LoadCustomPosition()
    {
        _hasCustomPosition = PlayerPrefs.HasKey(PrefX) && PlayerPrefs.HasKey(PrefY);
        if (_hasCustomPosition)
            _customViewport = new Vector2(PlayerPrefs.GetFloat(PrefX), PlayerPrefs.GetFloat(PrefY));
    }

    private void SaveCustomPosition()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 v = cam.WorldToViewportPoint(transform.position);
        _customViewport    = new Vector2(Mathf.Clamp01(v.x), Mathf.Clamp01(v.y));
        _hasCustomPosition = true;

        PlayerPrefs.SetFloat(PrefX, _customViewport.x);
        PlayerPrefs.SetFloat(PrefY, _customViewport.y);
        PlayerPrefs.Save();
        Debug.Log($"[SpinnerController] Moved by player to viewport {_customViewport}.");
    }

    /// <summary>Forgets the player's chosen spot and returns to automatic placement.</summary>
    public void ResetPosition()
    {
        _hasCustomPosition = false;
        PlayerPrefs.DeleteKey(PrefX);
        PlayerPrefs.DeleteKey(PrefY);
        PlayerPrefs.Save();
        Layout();
        Debug.Log("[SpinnerController] Position reset to automatic.");
    }

    // -- Input --------------------------------------------------------------
    // Tap = spin (on release) · Drag = move · Press and hold = reset position.

    void OnMouseDown()
    {
        if (_isSpinning) return;   // no moving or re-spinning mid-spin

        _pointerDown      = true;
        _dragging         = false;
        _longPressHandled = false;
        _pressTime        = Time.unscaledTime;
        _pressScreenPos   = Input.mousePosition;
        _grabOffset       = transform.position - PointerWorld();
    }

    void OnMouseDrag()   // called every frame while the press is held
    {
        if (!_pointerDown || _longPressHandled) return;

        if (!_dragging)
        {
            float dpi       = Screen.dpi > 0f ? Screen.dpi : 96f;
            float threshold = Mathf.Max(8f, dpi * DragThresholdInch);

            if ((Input.mousePosition - _pressScreenPos).magnitude < threshold)
            {
                if (Time.unscaledTime - _pressTime >= LongPressSeconds)
                {
                    _longPressHandled = true;
                    ResetPosition();
                }
                return;
            }
            _dragging = true;
        }

        Camera cam = Camera.main;
        if (cam != null)
            transform.position = ClampToScreen(cam, PointerWorld() + _grabOffset);
    }

    void OnMouseUp()   // fires even if released off the spinner
    {
        if (!_pointerDown) return;
        _pointerDown = false;

        if (_dragging)
        {
            _dragging = false;
            SaveCustomPosition();
            return;
        }
        if (_longPressHandled) return;

        if (Interactable) Spin();
    }

    private static Vector3 PointerWorld()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;
        Vector3 p = cam.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }

    // Hiding the spinner stops its coroutine; don't leave it stuck mid-spin or mid-drag.
    void OnDisable()
    {
        _isSpinning  = false;
        _pointerDown = false;
        _dragging    = false;

        // Put it down instantly so it doesn't reappear lifted.
        _lift = 1f;
        ApplyLift();
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
