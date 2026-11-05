using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The karma draw pile in the centre of the board.
///
///   PlayShuffleIntro(count)  At game start: cards fly in from off the board, land in a
///                            messy heap, get riffle-shuffled a few times (random cuts and
///                            random interleaving, so every game looks different) and are
///                            squared up into a neat deck. GameManager waits for it before
///                            the first turn.
///   TakeTopCard(worldPos)    When a card is drawn, the top card flies to the player's token
///                            and disappears, so the pile shrinks as the game goes on.
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
    private const int   FlyingOrder  = 8;        // a card being drawn passes over the pile

    // -- Timing (seconds) ---------------------------------------------------------------
    private const float DealTime    = 0.55f;
    private const float DealStagger = 0.035f;
    private const int   Riffles     = 3;
    private const float SplitTime   = 0.22f;
    private const float DropTime    = 0.14f;
    private const float DropStagger = 0.022f;
    private const float SquareTime  = 0.25f;
    private const float TakeTime    = 0.45f;

    private const float SplitDistance = 4.4f;    // how far the halves move apart (along the cards' long side)

    // card_back.png is 1024 x 1024 with the 1024 x 584 card centred (see tools/make_cards.py).
    private const int CardBackHeight = 584;

    private readonly List<SpriteRenderer> _pool  = new List<SpriteRenderer>();
    private readonly List<SpriteRenderer> _stack = new List<SpriteRenderer>();   // bottom → top
    private Sprite    _back;
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

    /// <summary>Moves the top card to <paramref name="worldTarget"/> (a player's token) and removes it.</summary>
    public IEnumerator TakeTopCard(Vector3 worldTarget)
    {
        if (_stack.Count == 0 || _board == null) yield break;

        var card = _stack[_stack.Count - 1];
        _stack.RemoveAt(_stack.Count - 1);
        card.sortingOrder = FlyingOrder;

        Vector3 target = transform.InverseTransformPoint(worldTarget);
        target.z = card.transform.localPosition.z;
        Vector3 start  = card.transform.localPosition;
        float   rot0   = card.transform.localEulerAngles.z;
        float   rot1   = rot0 + Random.Range(-90f, 90f);

        for (float t = 0f; t < 1f; t += Time.deltaTime / TakeTime)
        {
            float e = EaseInOut(t);
            Vector3 p = Vector3.Lerp(start, target, e);
            p.y += Mathf.Sin(e * Mathf.PI) * 1.5f;                       // small arc
            card.transform.localPosition = p;
            card.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(rot0, rot1, e));
            card.transform.localScale    = Vector3.one * Mathf.Lerp(1f, 0.35f, e);
            card.color = new Color(1f, 1f, 1f, Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0.6f, 1f, e)));
            yield return null;
        }
        card.gameObject.SetActive(false);
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
            var sr = _pool[i];
            sr.gameObject.SetActive(false);
            sr.color = Color.white;
            sr.transform.localScale = Vector3.one;
            if (i < count) _stack.Add(sr);
        }
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
