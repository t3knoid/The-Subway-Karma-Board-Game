using System.Collections;
using UnityEngine;

/// <summary>
/// Drives a Computer-type player's turn automatically:
///   1. Waits 0.8–1.2 s (UX feel).
///   2. Calls SpinnerController.SimulateSpin() for an RNG result.
///   3. Advances the token on the board.
///   4. Processes square effects (Station → draw card, etc.).
///   5. Calls GameManager.NextTurn() to return control.
///
/// Attach to any persistent GameObject and set playerState before the
/// first frame (GameSetupScreen does this).
/// </summary>
public class ComputerPlayer : MonoBehaviour
{
    // -- References ---------------------------------------------------------
    /// PlayerState owned by this CPU player. Set by GameSetupScreen.
    public PlayerState playerState;

    // -- Token visuals ------------------------------------------------------
    private const int   TokenTexRes  = 64;
    private const float TokenPPU     = 100f;
    private const float TokenOffsetX =  0.15f;
    private const float TokenOffsetY = -0.15f;
    private const float TokenZ       = -1f;

    private GameObject _token;

    // -- Turn state ---------------------------------------------------------
    private bool _skipNextTurn = false;   // set by LateTrain penalty

    // -- Lifecycle ----------------------------------------------------------
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (playerState == null)
        {
            Debug.LogError("[ComputerPlayer] playerState not assigned.");
            return;
        }
        CreateToken();
        PlaceToken(playerState.boardIndex);
    }

    // -- Token construction -------------------------------------------------

    private void CreateToken()
    {
        _token = new GameObject("ComputerToken");
        DontDestroyOnLoad(_token);

        var sr = _token.AddComponent<SpriteRenderer>();
        sr.sprite       = MakeCircleSprite(new Color(0.72f, 0.22f, 1f));  // vivid purple
        sr.sortingOrder = 20;
    }

    private static Sprite MakeCircleSprite(Color color)
    {
        int res = TokenTexRes;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        float cx = res * 0.5f - 0.5f;
        float cy = res * 0.5f - 0.5f;
        float r2 = cx * cx;

        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float dx = x - cx, dy = y - cy;
                tex.SetPixel(x, y, dx * dx + dy * dy <= r2 ? color : Color.clear);
            }

        tex.Apply();
        // Token world radius ≈ 0.3 world units
        float ppu = TokenPPU / (res * 0.3f / res);
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), ppu);
    }

    private void PlaceToken(int boardIndex)
    {
        if (_token == null) return;
        var zone = GameObject.Find($"TapZone_{boardIndex:D2}");
        if (zone == null) return;
        _token.transform.position =
            zone.transform.position + new Vector3(TokenOffsetX, TokenOffsetY, TokenZ);
    }

    // -- Public API ---------------------------------------------------------

    /// <summary>Called by GameManager when it is this CPU player's turn.</summary>
    public void TakeTurn() => StartCoroutine(TurnCoroutine());

    // -- Turn coroutine -----------------------------------------------------

    private IEnumerator TurnCoroutine()
    {
        // ─ Honour LateTrain penalty ──────────────────────────────────────────
        if (_skipNextTurn)
        {
            _skipNextTurn = false;
            Debug.Log("[ComputerPlayer] Skip turn (LateTrain penalty).");
            yield return new WaitForSeconds(0.8f);
            AdvanceTurn();
            yield break;
        }

        // ─ Thinking delay ───────────────────────────────────────────────────
        Debug.Log("[ComputerPlayer] Taking turn...");
        yield return new WaitForSeconds(Random.Range(0.8f, 1.2f));

        // ─ Simulate spin ────────────────────────────────────────────────────
        int steps = SpinnerController.Instance != null
            ? SpinnerController.Instance.SimulateSpin()
            : Random.Range(1, 7);
        Debug.Log($"[ComputerPlayer] Simulated spin: {steps}");

        // ─ Advance token ────────────────────────────────────────────────────
        playerState.boardIndex = (playerState.boardIndex + steps) % BoardManager.BoardSize;
        PlaceToken(playerState.boardIndex);

        // ─ Process landing square ───────────────────────────────────────────
        yield return ProcessSquare(playerState.boardIndex);

        Debug.Log($"[ComputerPlayer] Turn done — now at square {playerState.boardIndex}.");
        AdvanceTurn();
    }

    private IEnumerator ProcessSquare(int index)
    {
        if (BoardManager.Instance == null) yield break;

        var square = BoardManager.Instance.GetSquare(index);
        Debug.Log($"[ComputerPlayer] Landed on [{index}] {square.type}: {square.displayName}");

        switch (square.type)
        {
            // ── Station: draw karma card ─────────────────────────────────────
            case SquareType.Station:
            {
                var card = KarmaDeck.Instance?.DrawCard();
                if (card != null)
                {
                    playerState.AddCard(card);
                    Debug.Log($"[ComputerPlayer] Drew '{card.title}' ({card.points:+#;-#;0} karma).");
                }
                break;
            }

            // ── Early Train: move 1 extra space forward ──────────────────────
            case SquareType.EarlyTrain:
                playerState.boardIndex = (playerState.boardIndex + 1) % BoardManager.BoardSize;
                PlaceToken(playerState.boardIndex);
                Debug.Log($"[ComputerPlayer] Early Train — advanced to {playerState.boardIndex}.");
                yield return new WaitForSeconds(0.3f);
                // Apply landing effects for the extra square (no recursion guard needed;
                // EarlyTrain doesn't chain back to EarlyTrain by board design).
                yield return ProcessSquare(playerState.boardIndex);
                break;

            // ── Sick Passenger: go back 1 space ─────────────────────────────
            case SquareType.SickPassenger:
                playerState.boardIndex =
                    (playerState.boardIndex - 1 + BoardManager.BoardSize) % BoardManager.BoardSize;
                PlaceToken(playerState.boardIndex);
                Debug.Log($"[ComputerPlayer] Sick Passenger — moved back to {playerState.boardIndex}.");
                yield return new WaitForSeconds(0.3f);
                break;

            // ── Late Train: lose the NEXT turn ──────────────────────────────
            case SquareType.LateTrain:
                Debug.Log("[ComputerPlayer] Late Train — will skip next turn.");
                _skipNextTurn = true;
                yield return new WaitForSeconds(0.3f);
                break;

            // ── Express Train: spin again for bonus movement ─────────────────
            case SquareType.ExpressTrain:
            {
                yield return new WaitForSeconds(0.5f);
                int bonus = SpinnerController.Instance != null
                    ? SpinnerController.Instance.SimulateSpin()
                    : Random.Range(1, 7);
                Debug.Log($"[ComputerPlayer] Express Train! Bonus spin: {bonus}.");
                playerState.boardIndex = (playerState.boardIndex + bonus) % BoardManager.BoardSize;
                PlaceToken(playerState.boardIndex);
                // Process bonus landing square once (no recursive express chains).
                var bonusSquare = BoardManager.Instance.GetSquare(playerState.boardIndex);
                if (bonusSquare.type == SquareType.Station)
                {
                    var card = KarmaDeck.Instance?.DrawCard();
                    if (card != null) playerState.AddCard(card);
                }
                break;
            }

            case SquareType.NoOp:
            default:
                break;
        }
    }

    // -- Helpers ------------------------------------------------------------

    private void AdvanceTurn()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
            GameManager.Instance.NextTurn();
    }
}
