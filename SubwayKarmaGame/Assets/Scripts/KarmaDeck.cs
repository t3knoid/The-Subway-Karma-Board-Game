using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds the shuffled 26-card Karma deck (contents defined in StandardGame).
///
/// Deck is built and shuffled (Fisher-Yates) at game start and on Play Again.
/// Cards are drawn by TurnResolver through ICardSource; end of game is decided
/// by the resolver/TurnController, not by this component.
/// </summary>
public class KarmaDeck : MonoBehaviour, ICardSource
{
    // -- Events -------------------------------------------------------------
    /// Fired each time a card is drawn. Argument: the drawn card.
    public event Action<KarmaCard> OnCardDrawn;

    // -- State --------------------------------------------------------------
    public int RemainingCount => _deck.Count;
    int ICardSource.Remaining => _deck.Count;

    private readonly List<KarmaCard> _deck = new List<KarmaCard>();

    // -- Singleton ----------------------------------------------------------
    public static KarmaDeck Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildDeck();
    }

    // -- Public API ---------------------------------------------------------

    /// <summary>
    /// Rebuilds and reshuffles the deck. Call at game start and on Play Again.
    /// </summary>
    public void BuildDeck()
    {
        _deck.Clear();
        _deck.AddRange(StandardGame.CreateDeck());
        Shuffle();
        Debug.Log($"[KarmaDeck] Built and shuffled {_deck.Count} cards.");
    }

    /// <summary>
    /// Fisher-Yates shuffle in-place.
    /// </summary>
    public void Shuffle()
    {
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }

    /// <summary>
    /// Draws the top card from the deck and fires OnCardDrawn.
    /// Returns null if the deck was already empty.
    /// </summary>
    public KarmaCard Draw()
    {
        if (_deck.Count == 0)
        {
            Debug.LogWarning("[KarmaDeck] Draw called on empty deck.");
            return null;
        }

        KarmaCard card = _deck[_deck.Count - 1];
        _deck.RemoveAt(_deck.Count - 1);

        Debug.Log($"[KarmaDeck] Drew: {card}  (remaining: {_deck.Count})");
        OnCardDrawn?.Invoke(card);
        return card;
    }
}
