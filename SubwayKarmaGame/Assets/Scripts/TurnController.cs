using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Plays every turn, human or computer, through the same rules engine:
///
///   1. Skip the turn if the player owes a Late Train penalty.
///   2. Get a spin: humans tap the spinner, the computer rolls after a short pause.
///   3. TurnResolver works out the moves, cards and penalties.
///   4. Animate the resulting events and apply them to the PlayerState.
///   5. Express Train → get a second spin and repeat 3–4 with ResolveBonusSpin.
///   6. GameManager.NextTurn(), or TriggerEndGame() if the deck ran out.
///
/// The spinner is only visible while waiting for a human to spin.
/// </summary>
public class TurnController : MonoBehaviour
{
    public static TurnController Instance { get; private set; }

    // -- Pacing (seconds) ---------------------------------------------------
    private const float CpuThinkMin     = 0.8f;
    private const float CpuThinkMax     = 1.2f;
    private const float SpinResultPause = 0.5f;   // let the player read the spinner
    private const float CardPause       = 0.4f;
    private const float PenaltyPause    = 0.3f;
    private const float SkipTurnPause   = 0.8f;

    // -- State --------------------------------------------------------------
    private TurnResolver _resolver;
    private Coroutine    _turn;
    private bool         _awaitingSpin;
    private int?         _spinResult;
    private bool         _subscribed;

    // -- Lifecycle ----------------------------------------------------------
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnChanged += HandleTurnChanged;
        else
            Debug.LogError("[TurnController] No GameManager found.");

        if (SpinnerController.Instance != null)
        {
            SpinnerController.Instance.OnSpinComplete += HandleSpinComplete;
            _subscribed = true;
        }

        // Nothing can be spun until the first turn begins.
        SetSpinnerVisible(false);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnChanged -= HandleTurnChanged;
        if (_subscribed && SpinnerController.Instance != null)
            SpinnerController.Instance.OnSpinComplete -= HandleSpinComplete;
    }

    // -- Turn flow ----------------------------------------------------------

    private TurnResolver Resolver
    {
        get
        {
            if (_resolver == null)
                _resolver = new TurnResolver(BoardManager.Instance.Squares, KarmaDeck.Instance);
            return _resolver;
        }
    }

    private void HandleTurnChanged(PlayerState player)
    {
        if (_turn != null) StopCoroutine(_turn);
        _awaitingSpin = false;
        _turn = StartCoroutine(RunTurn(player));
    }

    private IEnumerator RunTurn(PlayerState player)
    {
        SetSpinnerVisible(false);

        if (KarmaDeck.Instance == null || KarmaDeck.Instance.RemainingCount == 0)
        {
            EndTurn(gameOver: true);
            yield break;
        }

        // ── Late Train penalty from a previous turn ─────────────────────────
        if (player.loseNextTurn)
        {
            player.loseNextTurn = false;
            Debug.Log($"[TurnController] {player.playerName} loses this turn (Late Train).");
            yield return new WaitForSeconds(SkipTurnPause);
            EndTurn(gameOver: false);
            yield break;
        }

        // ── Main spin ───────────────────────────────────────────────────────
        int spin = 0;
        yield return GetSpin(player, v => spin = v);
        Debug.Log($"[TurnController] {player.playerName} spun {spin}.");

        TurnResult result = Resolver.ResolveSpin(player.boardIndex, spin);
        yield return Play(player, result);
        bool gameOver = result.DeckExhausted;

        // ── Express Train bonus spin ────────────────────────────────────────
        if (result.NeedsBonusSpin && !gameOver)
        {
            yield return GetSpin(player, v => spin = v);
            Debug.Log($"[TurnController] {player.playerName} Express Train bonus spin: {spin}.");

            TurnResult bonus = Resolver.ResolveBonusSpin(player.boardIndex, spin);
            yield return Play(player, bonus);
            gameOver = bonus.DeckExhausted;
        }

        EndTurn(gameOver);
    }

    private void EndTurn(bool gameOver)
    {
        // Clear first: NextTurn() synchronously starts the next turn's coroutine.
        _turn = null;
        SetSpinnerVisible(false);

        if (GameManager.Instance == null) return;
        if (gameOver) GameManager.Instance.TriggerEndGame();
        else          GameManager.Instance.NextTurn();
    }

    // -- Spins --------------------------------------------------------------

    private IEnumerator GetSpin(PlayerState player, Action<int> setResult)
    {
        var spinner = SpinnerController.Instance;

        if (player.playerType == PlayerType.Computer || spinner == null)
        {
            if (spinner == null && player.playerType == PlayerType.Human)
                Debug.LogError("[TurnController] No spinner; rolling for the human player.");

            yield return new WaitForSeconds(UnityEngine.Random.Range(CpuThinkMin, CpuThinkMax));
            setResult(UnityEngine.Random.Range(TurnResolver.MinSpin, TurnResolver.MaxSpin + 1));
            yield break;
        }

        _spinResult         = null;
        _awaitingSpin       = true;
        spinner.Interactable = true;
        SetSpinnerVisible(true);

        while (!_spinResult.HasValue)
            yield return null;

        // Keep the result on screen briefly, but ignore further taps.
        _awaitingSpin        = false;
        spinner.Interactable = false;
        yield return new WaitForSeconds(SpinResultPause);
        SetSpinnerVisible(false);
        setResult(_spinResult.Value);
    }

    private void HandleSpinComplete(int value)
    {
        if (_awaitingSpin) _spinResult = value;
    }

    private static void SetSpinnerVisible(bool visible)
    {
        var spinner = SpinnerController.Instance;
        if (spinner != null && spinner.gameObject.activeSelf != visible)
            spinner.gameObject.SetActive(visible);
    }

    // -- Playback -----------------------------------------------------------

    /// <summary>Animates each rules event in order and applies it to the player.</summary>
    private IEnumerator Play(PlayerState player, TurnResult result)
    {
        foreach (var e in result.Events)
        {
            switch (e.Kind)
            {
                case TurnEventKind.Moved:
                    yield return AnimateMove(player, e.From, e.Steps);
                    player.boardIndex = e.To;
                    break;

                case TurnEventKind.CardDrawn:
                    player.AddCard(e.Card);
                    yield return new WaitForSeconds(CardPause);
                    break;

                case TurnEventKind.LoseNextTurn:
                    player.loseNextTurn = true;
                    Debug.Log($"[TurnController] {player.playerName} hit a Late Train — loses next turn.");
                    yield return new WaitForSeconds(PenaltyPause);
                    break;

                case TurnEventKind.BonusSpinRequired:
                    Debug.Log($"[TurnController] {player.playerName} caught an Express Train — spin again!");
                    break;

                case TurnEventKind.DeckExhausted:
                    Debug.Log("[TurnController] Last karma card drawn.");
                    break;
            }
        }

        player.boardIndex = result.Position;
    }

    /// <summary>Moves the token one square at a time so it follows the board around corners.</summary>
    private static IEnumerator AnimateMove(PlayerState player, int from, int steps)
    {
        if (player.Token == null) yield break;

        int dir = steps >= 0 ? 1 : -1;
        int n   = BoardManager.BoardSize;

        for (int i = 1; i <= Mathf.Abs(steps); i++)
        {
            int  index = ((from + dir * i) % n + n) % n;
            bool done  = false;
            player.Token.MoveTo(index, () => done = true);
            while (!done) yield return null;
        }
    }
}
