using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton that owns the list of active PlayerState objects and the
/// current turn index. For solitaire there is one player; multiplayer
/// adds more via RegisterPlayer().
///
/// Turn flow (to be driven by a future turn controller):
///   GameManager.Instance.CurrentPlayer  — whose turn it is
///   GameManager.Instance.NextTurn()     — advance to next player
/// </summary>
public class GameManager : MonoBehaviour
{
    // -- Singleton ----------------------------------------------------------
    public static GameManager Instance { get; private set; }

    // -- Events -------------------------------------------------------------
    /// Fired when the active player changes. Argument: new current player.
    public event Action<PlayerState> OnTurnChanged;

    /// Fired when a new player is registered.
    public event Action<PlayerState> OnPlayerRegistered;

    /// Fired when the game ends (deck exhausted). Argument: ranked player list.
    public event Action<List<PlayerState>> OnGameOver;

    // -- State --------------------------------------------------------------
    public IReadOnlyList<PlayerState> Players => _players;
    public int CurrentTurnIndex { get; private set; } = 0;
    public bool IsGameOver { get; private set; } = false;

    public PlayerState CurrentPlayer =>
        _players.Count > 0 ? _players[CurrentTurnIndex] : null;

    private readonly List<PlayerState> _players         = new List<PlayerState>();
    private ComputerPlayer             _computerPlayer;

    // -- Lifecycle ----------------------------------------------------------
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Subscribe to deck-exhausted event once KarmaDeck is ready.
        if (KarmaDeck.Instance != null)
            KarmaDeck.Instance.OnDeckExhausted += TriggerEndGame;
    }

    void OnDestroy()
    {
        if (KarmaDeck.Instance != null)
            KarmaDeck.Instance.OnDeckExhausted -= TriggerEndGame;
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

    /// <summary>
    /// Registers the ComputerPlayer controller used to auto-drive CPU turns.
    /// </summary>
    public void RegisterComputerPlayer(ComputerPlayer cp)
    {
        _computerPlayer = cp;
        Debug.Log($"[GameManager] ComputerPlayer registered.");
    }

    /// <summary>
    /// Advances to the next player's turn (wraps around).
    /// Enables the spinner for human turns; triggers CPU turns automatically.
    /// </summary>
    public void NextTurn()
    {
        if (_players.Count == 0) return;
        CurrentTurnIndex = (CurrentTurnIndex + 1) % _players.Count;
        Debug.Log($"[GameManager] Turn → {CurrentPlayer.playerName}");
        OnTurnChanged?.Invoke(CurrentPlayer);

        // Show spinner only on the human player's turn.
        var spinner = SpinnerController.Instance;
        if (spinner != null)
            spinner.gameObject.SetActive(CurrentPlayer.playerType == PlayerType.Human);

        // Auto-drive the computer's turn.
        if (CurrentPlayer.playerType == PlayerType.Computer && _computerPlayer != null)
            _computerPlayer.TakeTurn();
    }

    /// <summary>
    /// Resets all player states and restarts from player 0's turn.
    /// Call on Play Again.
    /// </summary>
    public void ResetGame()
    {
        IsGameOver = false;
        CurrentTurnIndex = 0;
        foreach (var p in _players) p.Reset();
        KarmaDeck.Instance?.BuildDeck();
        Debug.Log("[GameManager] Game reset.");
    }

    /// <summary>
    /// Called when the karma deck is exhausted. Sorts players by karmaTotal
    /// descending and fires OnGameOver with the ranked list.
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
