using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Identifies whether a player is human-controlled or CPU-controlled.
/// </summary>
public enum PlayerType { Human, Computer }

/// <summary>
/// Runtime state for one player (plain data, one instance per player):
///   - karmaTotal   : running sum of all drawn card points
///   - hand         : ordered list of every card drawn this game
///   - boardIndex   : current position on the 22-square board
///   - loseNextTurn : set by a Late Train square
///
/// Players are created by GameSetupScreen and owned by GameManager.
/// </summary>
public class PlayerState
{
    // -- Events -------------------------------------------------------------
    /// Fired after karmaTotal changes. Argument: new total.
    public event Action<int> OnKarmaChanged;

    // -- Identity -----------------------------------------------------------
    public readonly int        playerId;
    public readonly string     playerName;
    public readonly PlayerType playerType;

    // -- Token --------------------------------------------------------------
    /// <summary>Visual subway-car token for this player.</summary>
    public PlayerToken Token;

    // -- Board position / turn state ------------------------------------------
    public int  boardIndex   = 0;
    public bool loseNextTurn = false;

    // -- Karma totals -------------------------------------------------------
    public int             karmaTotal { get; private set; } = 0;
    public List<KarmaCard> hand       { get; private set; } = new List<KarmaCard>();

    public PlayerState(int playerId, string playerName, PlayerType playerType)
    {
        this.playerId   = playerId;
        this.playerName = playerName;
        this.playerType = playerType;
    }

    // -- Public API ---------------------------------------------------------

    /// <summary>
    /// Adds a drawn card to the player's hand and updates karmaTotal.
    /// Special cards (InstantKarma / NoOp) contribute 0 points.
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
        boardIndex   = 0;
        loseNextTurn = false;
        karmaTotal   = 0;
        hand.Clear();
        OnKarmaChanged?.Invoke(karmaTotal);
        Debug.Log($"[PlayerState] {playerName} state reset.");
    }
}
