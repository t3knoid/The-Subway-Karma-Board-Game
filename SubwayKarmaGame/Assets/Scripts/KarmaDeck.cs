using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the 26-card Karma deck (10 positive, 10 negative, 6 special).
/// 
/// Deck is built and shuffled (Fisher-Yates) at game start and on Play Again.
/// Cards are drawn one at a time from the top; OnDeckExhausted fires when
/// the last card is drawn.
///
/// Special card behaviour in solitaire:
///   InstantKarma / NoOp — 0 points, display "No effect in solo play", end turn.
/// </summary>
public class KarmaDeck : MonoBehaviour
{
    // -- Events -------------------------------------------------------------
    /// Fired each time a card is drawn. Argument: the drawn card.
    public event Action<KarmaCard> OnCardDrawn;

    /// Fired when the last card in the deck is drawn.
    public event Action OnDeckExhausted;

    // -- State --------------------------------------------------------------
    public int RemainingCount => _deck.Count;

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

        // ── Positive cards (karma1.jpg) ─────────────────────────────────────
        _deck.Add(new KarmaCard("Finds Metrocard",                   +5));
        _deck.Add(new KarmaCard("Gets a Seat",                       +1));
        _deck.Add(new KarmaCard("Holds Door for Someone",            +2));
        _deck.Add(new KarmaCard("Gives Seat to Elderly",             +9));
        _deck.Add(new KarmaCard("Gives Seat to Pregnant Woman",      +9));
        _deck.Add(new KarmaCard("Seats Next to Celebrity",           +4));
        _deck.Add(new KarmaCard("Finds Money",                       +5));
        _deck.Add(new KarmaCard("Gives Money to Musician",           +2));
        _deck.Add(new KarmaCard("Local Train Becomes Express",       +1));
        _deck.Add(new KarmaCard("Finished Crossword",                +1));

        // ── Negative cards (karma2.jpg) ─────────────────────────────────────
        _deck.Add(new KarmaCard("Loses Metrocard",                              -5));
        _deck.Add(new KarmaCard("Overcrowded Train",                            -1));
        _deck.Add(new KarmaCard("Fell Asleep, Got Robbed, and Woke Up Naked",  -9));
        _deck.Add(new KarmaCard("Does Not Give Up Seat to Elderly",            -9));
        _deck.Add(new KarmaCard("Rats!",                                        -1));
        _deck.Add(new KarmaCard("Loses Wallet",                                -5));
        _deck.Add(new KarmaCard("Smelly Subway Car",                           -6));
        _deck.Add(new KarmaCard("Coffee Spills On You",                        -7));
        _deck.Add(new KarmaCard("Miss Your Stop",                              -1));
        _deck.Add(new KarmaCard("Does Not Give Up Seat to Pregnant Woman",     -9));

        // ── Special cards (karma3.jpg) ──────────────────────────────────────
        // 3x Instant Karma, 3x Says Hello — totalling 6 special cards.
        _deck.Add(new KarmaCard("Instant Karma",             0, KarmaCardType.InstantKarma));
        _deck.Add(new KarmaCard("Instant Karma",             0, KarmaCardType.InstantKarma));
        _deck.Add(new KarmaCard("Instant Karma",             0, KarmaCardType.InstantKarma));
        _deck.Add(new KarmaCard("Says Hello to Other Riders", 0, KarmaCardType.NoOp));
        _deck.Add(new KarmaCard("Says Hello to Other Riders", 0, KarmaCardType.NoOp));
        _deck.Add(new KarmaCard("Says Hello to Other Riders", 0, KarmaCardType.NoOp));

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
    /// Draws the top card from the deck.
    /// Fires OnCardDrawn, then OnDeckExhausted if the deck is now empty.
    /// Returns null if the deck was already empty.
    /// </summary>
    public KarmaCard DrawCard()
    {
        if (_deck.Count == 0)
        {
            Debug.LogWarning("[KarmaDeck] DrawCard called on empty deck.");
            return null;
        }

        KarmaCard card = _deck[_deck.Count - 1];
        _deck.RemoveAt(_deck.Count - 1);

        Debug.Log($"[KarmaDeck] Drew: {card}  (remaining: {_deck.Count})");
        OnCardDrawn?.Invoke(card);

        if (_deck.Count == 0)
            OnDeckExhausted?.Invoke();

        return card;
    }
}
