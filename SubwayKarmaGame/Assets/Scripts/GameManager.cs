using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton that owns the list of players and whose turn it is.
///
/// It does not play turns itself: TurnController listens to OnTurnChanged,
/// plays the turn (human or computer) and then calls NextTurn() or
/// TriggerEndGame().
/// </summary>
public class GameManager : MonoBehaviour
{
    // -- Singleton ----------------------------------------------------------
    public static GameManager Instance { get; private set; }

    // -- Events -------------------------------------------------------------
    /// Fired when a turn begins (including the first). Argument: player to move.
    public event Action<PlayerState> OnTurnChanged;

    /// Fired when a new player is registered.
    public event Action<PlayerState> OnPlayerRegistered;

    /// Fired when the game ends (deck exhausted). Argument: ranked player list.
    public event Action<List<PlayerState>> OnGameOver;

    // -- State --------------------------------------------------------------
    public IReadOnlyList<PlayerState> Players => _players;
    public int  CurrentTurnIndex { get; private set; } = 0;
    public bool IsGameOver       { get; private set; } = false;

    public PlayerState CurrentPlayer =>
        _players.Count > 0 ? _players[CurrentTurnIndex] : null;

    private readonly List<PlayerState> _players = new List<PlayerState>();

    // -- Lifecycle ----------------------------------------------------------
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -- Public API ---------------------------------------------------------

    /// <summary>
    /// Registers a player. Safe to call multiple times for the same player.
    /// </summary>
    public void RegisterPlayer(PlayerState player)
    {
        if (_players.Contains(player)) return;
        _players.Add(player);
        Debug.Log($"[GameManager] Registered player '{player.playerName}' " +
                  $"(id={player.playerId}). Total players: {_players.Count}");
        OnPlayerRegistered?.Invoke(player);
    }

    /// <summary>Starts play with player 0's turn.</summary>
    public void StartGame()
    {
        if (_players.Count == 0)
        {
            Debug.LogError("[GameManager] StartGame called with no players.");
            return;
        }
        IsGameOver = false;
        CurrentTurnIndex = 0;

        if (_startRoutine != null) StopCoroutine(_startRoutine);
        _startRoutine = StartCoroutine(DealThenStart());
    }

    /// <summary>True while the opening deal-and-shuffle animation is playing.</summary>
    public bool IsDealing { get; private set; }

    private Coroutine _startRoutine;

    private System.Collections.IEnumerator DealThenStart()
    {
        // Deal and shuffle the karma cards onto the centre of the board first.
        var deckView = KarmaDeckView.Instance;
        if (deckView != null && KarmaDeck.Instance != null)
        {
            IsDealing = true;
            yield return deckView.PlayShuffleIntro(KarmaDeck.Instance.RemainingCount);
            IsDealing = false;
        }

        _startRoutine = null;
        Debug.Log($"[GameManager] Game started. First turn: {CurrentPlayer.playerName}");
        OnTurnChanged?.Invoke(CurrentPlayer);
    }

    /// <summary>Advances to the next player's turn (wraps around).</summary>
    public void NextTurn()
    {
        if (_players.Count == 0 || IsGameOver) return;
        CurrentTurnIndex = (CurrentTurnIndex + 1) % _players.Count;
        Debug.Log($"[GameManager] Turn → {CurrentPlayer.playerName}");
        OnTurnChanged?.Invoke(CurrentPlayer);
    }

    /// <summary>
    /// Resets all players and the deck, then starts a new game. Call on Play Again.
    /// </summary>
    public void ResetGame()
    {
        foreach (var p in _players)
        {
            p.Reset();
            p.Token?.SnapTo(p.boardIndex);
        }
        KarmaDeck.Instance?.BuildDeck();
        Debug.Log("[GameManager] Game reset.");
        StartGame();
    }

    /// <summary>
    /// Called by TurnController once the turn that drew the last card has
    /// finished animating. Ranks players by karmaTotal and fires OnGameOver.
    /// </summary>
    public void TriggerEndGame()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        var ranked = _players.OrderByDescending(p => p.karmaTotal).ToList();
        Debug.Log("[GameManager] Game over. Rankings:");
        for (int i = 0; i < ranked.Count; i++)
            Debug.Log($"  #{i + 1}  {ranked[i].playerName}: {ranked[i].karmaTotal} karma");

        OnGameOver?.Invoke(ranked);
    }
}
