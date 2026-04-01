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

    // -- Token -------------------------------------------------------------
    private PlayerToken _token;

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
        // Create CPU token (slot 1 = Crimson); store on playerState so GameManager can snap it on reset.
        _token = PlayerToken.Create(playerState.playerId, playerState.boardIndex);
        playerState.Token = _token;
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
        yield return AnimateToken(playerState.boardIndex);

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
                yield return AnimateToken(playerState.boardIndex);
                Debug.Log($"[ComputerPlayer] Early Train — advanced to {playerState.boardIndex}.");
                yield return new WaitForSeconds(0.1f);
                // Apply landing effects for the extra square (no recursion guard needed;
                // EarlyTrain doesn't chain back to EarlyTrain by board design).
                yield return ProcessSquare(playerState.boardIndex);
                break;

            // ── Sick Passenger: go back 1 space ─────────────────────────────
            case SquareType.SickPassenger:
                playerState.boardIndex =
                    (playerState.boardIndex - 1 + BoardManager.BoardSize) % BoardManager.BoardSize;
                yield return AnimateToken(playerState.boardIndex);
                Debug.Log($"[ComputerPlayer] Sick Passenger — moved back to {playerState.boardIndex}.");
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
                yield return AnimateToken(playerState.boardIndex);
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

    private IEnumerator AnimateToken(int boardIndex)
    {
        if (_token == null) { yield return new WaitForSeconds(0.18f); yield break; }
        bool done = false;
        _token.MoveTo(boardIndex, () => done = true);
        yield return new UnityEngine.WaitUntil(() => done);
    }

    private void AdvanceTurn()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
            GameManager.Instance.NextTurn();
    }
}
