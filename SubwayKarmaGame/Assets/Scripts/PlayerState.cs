using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the runtime state for a single player:
///   - karmaTotal  : running sum of all drawn card points
///   - hand        : ordered list of every card drawn this game
///   - boardIndex  : current position on the 22-square board
///
/// In solitaire there is one PlayerState instance (player 0).
/// In multiplayer, create one per player and identify by playerId.
/// </summary>
public class PlayerState : MonoBehaviour
{
    // -- Events -------------------------------------------------------------
    /// Fired after karmaTotal changes. Argument: new total.
    public event Action<int> OnKarmaChanged;

    // -- Singleton (solitaire) ----------------------------------------------
    public static PlayerState Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -- Identity -----------------------------------------------------------
    public int    playerId   = 0;
    public string playerName = "Player";

    // -- Board position -----------------------------------------------------
    public int boardIndex = 0;

    // -- Karma totals -------------------------------------------------------
    public int            karmaTotal { get; private set; } = 0;
    public List<KarmaCard> hand      { get; private set; } = new List<KarmaCard>();

    // -- Public API ---------------------------------------------------------

    /// <summary>
    /// Adds a drawn card to the player's hand and updates karmaTotal.
    /// Special solitaire cards (InstantKarma / NoOp) contribute 0 points.
    /// </summary>
    public void AddCard(KarmaCard card)
    {
        hand.Add(card);

        int pts = (card.cardType == KarmaCardType.Standard) ? card.points : 0;
        karmaTotal += pts;

        Debug.Log($"[PlayerState] {playerName} drew '{card.title}' " +
                  $"({(pts >= 0 ? "+" : "")}{pts}) — total karma: {karmaTotal}");

        OnKarmaChanged?.Invoke(karmaTotal);
    }

    /// <summary>Resets state for a new game or Play Again.</summary>
    public void Reset()
    {
        boardIndex = 0;
        karmaTotal = 0;
        hand.Clear();
        Debug.Log($"[PlayerState] {playerName} state reset.");
    }
}
