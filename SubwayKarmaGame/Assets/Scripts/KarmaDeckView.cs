using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// The karma draw pile in the centre of the board.
///
///   PlayShuffleIntro(count)  At game start: cards fly in from off the board, land in a
///                            messy heap, get riffle-shuffled a few times (random cuts and
///                            random interleaving, so every game looks different) and are
///                            squared up into a neat deck. GameManager waits for it before
///                            the first turn.
///   RevealCard(card, token)  When a card is drawn, the top card lifts off the pile, flies to
///                            the middle of the screen, grows large and flips over to show its
///                            face (Resources/CardFaces/{slug}.png). It waits for a click
///                            (human) or a few seconds (computer), then flies into the
///                            player's subway car. The pile shrinks as the game goes on.
///
/// Purely visual: the real order is KarmaDeck's Fisher-Yates shuffle. Cards are children
/// of the board, so they follow it when the screen rotates or resizes.
/// </summary>
public class KarmaDeckView : MonoBehaviour
{
    public static KarmaDeckView Instance { get; private set; }

    // -- Size and stacking (board-local units; the board image is 24.11 x 28.35) --------
    // The pile sits on the board's tilted title card ("The Subway KARMA Game"), drawn by
    // tools/make_board.py: 980 x 490 px, rotated 32° anticlockwise, centred at board pixel
    // (1206, 1275) → board-local (0.0, 1.42). Keep these in step if that graphic moves.
    private static readonly Vector2 PileCentre = new Vector2(0f, 1.42f);
    private const float PileAngle    = 32f;      // degrees anticlockwise, same as the title card

    private const float CardWidth    = 6.4f;     // fits on the 9.8 x 4.9 title card; ≈ 2.3 world units
    private const float StackStep    = 0.012f;   // each card sits slightly higher → pile has some thickness
    private const float ZStep        = 0.002f;   // later cards drawn on top (orthographic z sort)
    private const int   SortingOrder = 5;        // above board (0) and tap zones (1), below spinner (10)

    // -- Timing (seconds) ---------------------------------------------------------------
    private const float DealTime    = 0.55f;
    private const float DealStagger = 0.035f;
    private const int   Riffles     = 3;
    private const float SplitTime   = 0.22f;
    private const float DropTime    = 0.14f;
    private const float DropStagger = 0.022f;
    private const float SquareTime  = 0.25f;

    // -- Card reveal -------------------------------------------------------------------
    private const float RevealWidth    = 5.6f;   // world units (the screen is 10 tall), easy to read
    private const float RevealZ        = -3f;    // in front of tokens (-0.5) and the spinner (-0.6 / -1)
    private const int   RevealOrder    = 30;     // drawn above everything on the board
    private const int   DimOrder       = 29;
    private const float DimAlpha       = 0.5f;
    private const float LiftTime       = 0.2f;
    private const float ToCentreTime   = 0.45f;
    private const float FlipTime       = 0.34f;
    private const float CollectTime    = 0.5f;
    private const float CpuShowSeconds = 2.5f;   // computer's cards collect themselves (a click skips)

    private const float SplitDistance = 4.4f;    // how far the halves move apart (along the cards' long side)

    // card_back.png is 1024 x 1024 with the 1024 x 584 card centred (see tools/make_cards.py).
    private const int CardBackHeight = 584;

    private readonly List<SpriteRenderer> _pool  = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> _stack = new List<SpriteRenderer>();   // bottom → top
    private Sprite    _back;
    private readonly Dictionary<string, Sprite> _faces = new Dictionary<string, Sprite>();
    private SpriteRenderer _dim;
    private Transform _board;
    private int       _running;   // active card tweens

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;   // re-parented under the board in EnsureReady()
    }

    // -- Public API ---------------------------------------------------------------------

    /// <summary>Deals <paramref name="count"/> cards into the centre and shuffles them.</summary>
    public IEnumerator PlayShuffleIntro(int count)
    {
        if (!EnsureReady() || count <= 0) yield break;

        StopAllCoroutines();
        _running = 0;
        ResetCards(count);

        // 1. Deal in: each card flies from a random point off the board, spinning, into a heap.
        for (int i = 0; i < _stack.Count; i++)
        {
            var card = _stack[i];
            card.gameObject.SetActive(true);
            card.sortingOrder = SortingOrder;
            Vector3 from = RandomOffBoard();
            from.z = StackZ(i);
            float spin = Random.Range(-540f, 540f);             // 1–1.5 turns while flying in
            card.transform.localPosition = from;
            card.transform.localRotation = Quaternion.Euler(0f, 0f, spin);

            Vector3 heap = new Vector3(Random.Range(-1.2f, 1.2f), Random.Range(-0.8f, 0.8f), StackZ(i));
            Tween(card, heap, Random.Range(-35f, 35f), DealTime, i * DealStagger, EaseOutBack, spin);
        }
        yield return WaitForTweens();

        // 2. Riffle shuffles with random cuts and random interleaving.
        for (int r = 0; r < Riffles; r++)
            yield return Riffle();

        // 3. Square up into a neat deck.
        for (int i = 0; i < _stack.Count; i++)
            Tween(_stack[i], StackPosition(i), Random.Range(-2f, 2f), SquareTime, i * 0.004f, EaseOut);
        yield return WaitForTweens();
    }

    /// <summary>
    /// Shows a drawn card: the top card comes off the pile to the middle of the screen,
    /// flips to its face, waits (for a click when <paramref name="waitForClick"/>, otherwise a
    /// few seconds or a click), then flies into <paramref name="token"/>.
    /// </summary>
    public IEnumerator RevealCard(KarmaCard card, Transform token, bool waitForClick)
    {
        if (_stack.Count == 0 || _board == null) yield break;
        Camera cam = Camera.main;

        var sr = _stack[_stack.Count - 1];
        _stack.RemoveAt(_stack.Count - 1);
        Transform t = sr.transform;

        // Leave the tilted, board-scaled pile: from here on, animate in world space.
        t.SetParent(null, true);
        sr.sortingOrder = RevealOrder;
        float bigScale  = RevealWidth / CardWidth;           // sprite is CardWidth units wide at scale 1
        Vector3 centre  = cam != null ? new Vector3(cam.transform.position.x, cam.transform.position.y, RevealZ)
                                      : new Vector3(0f, 0f, RevealZ);
        ShowDim(cam);

        // 1. Lift off the pile.
        Vector3 p0 = t.position;
        Vector3 lifted = new Vector3(p0.x, p0.y + 0.3f, RevealZ);
        float s0 = t.localScale.x;
        yield return Animate(LiftTime, e =>
        {
            t.position   = Vector3.Lerp(p0, lifted, e);
            t.localScale = Vector3.one * Mathf.Lerp(s0, s0 * 1.15f, e);
            SetDim(DimAlpha * 0.4f * e);
        }, EaseOut);

        // 2. Fly to the middle of the screen, straighten up and grow.
        float rot0 = t.eulerAngles.z, s1 = t.localScale.x;
        yield return Animate(ToCentreTime, e =>
        {
            t.position   = Vector3.Lerp(lifted, centre, e);
            t.rotation   = Quaternion.Euler(0f, 0f, rot0 + Mathf.DeltaAngle(rot0, 0f) * e);
            t.localScale = Vector3.one * Mathf.Lerp(s1, bigScale, e);
            SetDim(DimAlpha * (0.4f + 0.6f * e));
        }, EaseInOut);

        // 3. Flip over: squash to edge-on, swap to the face, open back up.
        Sprite face = FaceFor(card);
        yield return Animate(FlipTime, e =>
        {
            float open = Mathf.Abs(Mathf.Cos(e * Mathf.PI));       // 1 -> 0 -> 1
            if (e >= 0.5f && face != null) sr.sprite = face;
            t.localScale = new Vector3(bigScale * open, bigScale * (1f + 0.06f * Mathf.Sin(e * Mathf.PI)), 1f);
        }, x => x);
        t.localScale = Vector3.one * bigScale;

        // 4. Wait for the player to click the card (or a few seconds for the computer).
        yield return null;                                          // ignore a click from this same frame
        float waited = 0f;
        while (!ClickedOn(sr, cam))
        {
            if (!waitForClick && (waited += Time.deltaTime) >= CpuShowSeconds) break;
            yield return null;
        }

        // 5. Into the subway car: shrink, spin a little and fade as it arrives.
        Vector3 from = t.position;
        Vector3 to   = token != null ? new Vector3(token.position.x, token.position.y, RevealZ) : centre;
        float spin   = Random.Range(-120f, 120f);
        yield return Animate(CollectTime, e =>
        {
            t.position   = Vector3.Lerp(from, to, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 0.6f;
            t.rotation   = Quaternion.Euler(0f, 0f, spin * e);
            t.localScale = Vector3.one * Mathf.Lerp(bigScale, bigScale * 0.08f, e);
            sr.color     = new Color(1f, 1f, 1f, 1f - Mathf.InverseLerp(0.65f, 1f, e));
            SetDim(DimAlpha * (1f - e));
        }, EaseInOut);

        HideDim();
        RestoreToPool(sr);
    }

    // -- Shuffle ------------------------------------------------------------------------

    private IEnumerator Riffle()
    {
        int n   = _stack.Count;
        int cut = Mathf.Clamp(n / 2 + Random.Range(-3, 4), 1, n - 1);
        var left  = _stack.GetRange(0, cut);
        var right = _stack.GetRange(cut, n - cut);

        // Split: the two halves slide apart and tilt.
        float tiltL = Random.Range(6f, 14f), tiltR = -Random.Range(6f, 14f);
        float lift  = Random.Range(-0.4f, 0.4f);
        for (int i = 0; i < left.Count; i++)
            Tween(left[i], new Vector3(-SplitDistance, lift + i * StackStep, StackZ(i)), tiltL, SplitTime, 0f, EaseOut);
        for (int i = 0; i < right.Count; i++)
            Tween(right[i], new Vector3(SplitDistance, -lift + i * StackStep, StackZ(i)), tiltR, SplitTime, 0f, EaseOut);
        yield return WaitForTweens();

        // Interleave: cards drop from the bottom of either half, chosen at random in
        // proportion to how many are left in each (Gilbert–Shannon–Reeds riffle model).
        var merged = new List<SpriteRenderer>(n);
        int a = 0, b = 0;
        while (a < left.Count || b < right.Count)
        {
            int leftLeft = left.Count - a, rightLeft = right.Count - b;
            bool takeLeft = Random.Range(0, leftLeft + rightLeft) < leftLeft;
            merged.Add(takeLeft ? left[a++] : right[b++]);
        }
        for (int i = 0; i < merged.Count; i++)
        {
            var card = merged[i];
            Vector3 p = card.transform.localPosition;
            card.transform.localPosition = new Vector3(p.x, p.y, StackZ(i));   // new draw order now
            Vector3 dest = StackPosition(i) + new Vector3(Random.Range(-0.25f, 0.25f), 0f, 0f);
            Tween(card, dest, Random.Range(-5f, 5f), DropTime, i * DropStagger, EaseOut);
        }
        _stack.Clear();
        _stack.AddRange(merged);
        yield return WaitForTweens();
    }

    // -- Setup --------------------------------------------------------------------------

    private bool EnsureReady()
    {
        if (_board == null)
        {
            var display = FindAnyObjectByType<BoardDisplay>();
            if (display == null) return false;
            _board = display.transform;
            transform.SetParent(_board, false);
            // Everything below is in this tilted frame: stacking, riffles and flights all follow the angle.
            transform.localPosition = new Vector3(PileCentre.x, PileCentre.y, -0.05f);
            transform.localRotation = Quaternion.Euler(0f, 0f, PileAngle);
            transform.localScale    = Vector3.one;
        }
        if (_back == null) _back = LoadCardBack();
        return _back != null;
    }

    private static Sprite LoadCardBack()
    {
        var tex = Resources.Load<Texture2D>("card_back");
        if (tex != null)
        {
            float h = CardBackHeight * tex.height / 1024f;   // in case the texture was resized on import
            var rect = new Rect(0f, (tex.height - h) * 0.5f, tex.width, h);
            return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), tex.width / CardWidth);
        }

        Debug.LogWarning("[KarmaDeckView] Resources/card_back.png not found; using plain card backs.");
        var plain = new Texture2D(35, 20);
        var fill  = new Color32(0x8e, 0xb6, 0x10, 0xff);
        var px    = new Color32[35 * 20];
        for (int i = 0; i < px.Length; i++) px[i] = fill;
        plain.SetPixels32(px);
        plain.Apply();
        return Sprite.Create(plain, new Rect(0, 0, 35, 20), new Vector2(0.5f, 0.5f), 35f / CardWidth);
    }

    private void ResetCards(int count)
    {
        while (_pool.Count < count)
        {
            var go = new GameObject($"KarmaCardBack_{_pool.Count:D2}");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _back;
            _pool.Add(sr);
        }

        _stack.Clear();
        for (int i = 0; i < _pool.Count; i++)
        {
            RestoreToPool(_pool[i]);
            if (i < count) _stack.Add(_pool[i]);
        }
        HideDim();
    }

    /// <summary>Puts a card back in the pile's frame, face down and hidden, ready to deal again.</summary>
    private void RestoreToPool(SpriteRenderer sr)
    {
        sr.transform.SetParent(transform, false);
        sr.transform.localScale    = Vector3.one;
        sr.transform.localRotation = Quaternion.identity;
        sr.sprite       = _back;
        sr.color        = Color.white;
        sr.sortingOrder = SortingOrder;
        sr.gameObject.SetActive(false);
    }

    // -- Card faces ---------------------------------------------------------------------

    /// <summary>File name for a card face; must match slug() in tools/make_cards.py.</summary>
    public static string Slug(string title)
    {
        var sb = new System.Text.StringBuilder();
        bool gap = false;
        foreach (char ch in title.ToLowerInvariant())
        {
            bool alnum = (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9');
            if (!alnum) { gap = true; continue; }
            if (gap && sb.Length > 0) sb.Append('_');
            sb.Append(ch);
            gap = false;
        }
        return sb.ToString();
    }

    private Sprite FaceFor(KarmaCard card)
    {
        string key = Slug(card.title);
        if (_faces.TryGetValue(key, out var cached)) return cached;

        var tex = Resources.Load<Texture2D>("CardFaces/" + key);
        Sprite sprite = null;
        if (tex != null)
        {
            float h = CardBackHeight * tex.height / 1024f;
            sprite = Sprite.Create(tex, new Rect(0f, (tex.height - h) * 0.5f, tex.width, h),
                                   new Vector2(0.5f, 0.5f), tex.width / CardWidth);
        }
        else
        {
            Debug.LogWarning($"[KarmaDeckView] No face image Resources/CardFaces/{key}.png for '{card.title}' " +
                             "(run tools/make_cards.py). Showing the card back.");
        }
        _faces[key] = sprite;
        return sprite;
    }

    // -- Dimmed backdrop during a reveal (also blocks clicks on the board) -------------

    private void ShowDim(Camera cam)
    {
        if (_dim == null)
        {
            var go = new GameObject("CardRevealDim");
            _dim = go.AddComponent<SpriteRenderer>();
            var tex = Texture2D.whiteTexture;
            _dim.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            _dim.sortingOrder = DimOrder;
            go.AddComponent<BoxCollider2D>().size = Vector2.one;   // nearest collider, so board squares don't get the click
        }
        float h = cam != null ? cam.orthographicSize * 2f + 2f : 12f;
        float w = cam != null ? h * cam.aspect + 2f : 24f;
        Vector3 c = cam != null ? cam.transform.position : Vector3.zero;
        _dim.transform.position   = new Vector3(c.x, c.y, RevealZ + 0.5f);
        _dim.transform.localScale = new Vector3(w, h, 1f);
        SetDim(0f);
        _dim.gameObject.SetActive(true);
    }

    private void SetDim(float alpha)
    {
        if (_dim != null) _dim.color = new Color(0f, 0f, 0f, alpha);
    }

    private void HideDim()
    {
        if (_dim != null) _dim.gameObject.SetActive(false);
    }

    // -- Pointer ------------------------------------------------------------------------

    /// <summary>True on the frame the mouse/touch is pressed over <paramref name="sr"/>.</summary>
    private static bool ClickedOn(SpriteRenderer sr, Camera cam)
    {
        if (cam == null) return false;
        Vector2 screen = Vector2.zero;
        bool pressed = false;
        bool read = false;
#if ENABLE_INPUT_SYSTEM
        var pointer = Pointer.current;
        if (pointer != null)
        {
            pressed = pointer.press.wasPressedThisFrame;
            screen  = pointer.position.ReadValue();
            read    = true;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (!read)
        {
            pressed = Input.GetMouseButtonDown(0);
            screen  = Input.mousePosition;
        }
#endif
        if (!pressed) return false;

        Vector3 w = cam.ScreenToWorldPoint(screen);
        Bounds b  = sr.bounds;
        return w.x >= b.min.x && w.x <= b.max.x && w.y >= b.min.y && w.y <= b.max.y;
    }

    // -- Simple time-based animation helper ---------------------------------------------

    private static IEnumerator Animate(float duration, System.Action<float> step, Ease ease)
    {
        for (float k = 0f; k < 1f; k += Time.deltaTime / duration)
        {
            step(ease(Mathf.Clamp01(k)));
            yield return null;
        }
        step(1f);
    }

    private static Vector3 StackPosition(int i) => new Vector3(0f, i * StackStep, StackZ(i));
    private static float   StackZ(int i)        => -(i + 1) * ZStep;

    /// <summary>A random point just outside the board, in any direction.</summary>
    private static Vector3 RandomOffBoard()
    {
        // The board's half-diagonal is ~18.6 local units; 21 from the (tilted, slightly
        // off-centre) pile is always outside the board, whatever the pile's angle.
        float a = Random.Range(0f, 2f * Mathf.PI);
        return new Vector3(Mathf.Cos(a) * 21f, Mathf.Sin(a) * 21f, 0f);
    }

    // -- Tweening -----------------------------------------------------------------------

    private delegate float Ease(float t);

    /// <param name="rotFrom">Start angle to spin from (full turns allowed); null = current angle, shortest way.</param>
    private void Tween(SpriteRenderer card, Vector3 to, float rotTo, float duration, float delay, Ease ease,
                       float? rotFrom = null)
    {
        _running++;
        StartCoroutine(TweenRoutine(card.transform, to, rotTo, duration, delay, ease, rotFrom));
    }

    private IEnumerator TweenRoutine(Transform t, Vector3 to, float rotTo, float duration, float delay, Ease ease,
                                     float? rotFrom)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        Vector3 from = t.localPosition;
        float   rot0 = rotFrom ?? t.localEulerAngles.z;
        for (float k = 0f; k < 1f; k += Time.deltaTime / duration)
        {
            float e = ease(Mathf.Clamp01(k));
            float angle = rotFrom.HasValue ? Mathf.LerpUnclamped(rot0, rotTo, e)
                                           : rot0 + Mathf.DeltaAngle(rot0, rotTo) * e;
            t.localPosition = Vector3.LerpUnclamped(from, to, e);
            t.localRotation = Quaternion.Euler(0f, 0f, angle);
            yield return null;
        }
        t.localPosition = to;
        t.localRotation = Quaternion.Euler(0f, 0f, rotTo);
        _running--;
    }

    private IEnumerator WaitForTweens()
    {
        while (_running > 0) yield return null;
    }

    private static float EaseOut(float t)   => 1f - (1f - t) * (1f - t);
    private static float EaseInOut(float t) => t * t * (3f - 2f * t);
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.4f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
