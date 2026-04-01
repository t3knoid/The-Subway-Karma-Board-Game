using System;
using System.Collections.Generic;
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

    // -- State --------------------------------------------------------------
    public IReadOnlyList<PlayerState> Players => _players;
    public int CurrentTurnIndex { get; private set; } = 0;

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

    /// <summary>
    /// Advances to the next player's turn (wraps around).
    /// </summary>
    public void NextTurn()
    {
        if (_players.Count == 0) return;
        CurrentTurnIndex = (CurrentTurnIndex + 1) % _players.Count;
        Debug.Log($"[GameManager] Turn → {CurrentPlayer.playerName}");
        OnTurnChanged?.Invoke(CurrentPlayer);
    }

    /// <summary>
    /// Resets all player states and restarts from player 0's turn.
    /// Call on Play Again.
    /// </summary>
    public void ResetGame()
    {
        CurrentTurnIndex = 0;
        foreach (var p in _players) p.Reset();
        KarmaDeck.Instance?.BuildDeck();
        Debug.Log("[GameManager] Game reset.");
    }
}
