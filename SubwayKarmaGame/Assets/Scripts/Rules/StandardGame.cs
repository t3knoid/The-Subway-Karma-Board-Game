using System.Collections.Generic;

/// <summary>
/// The contents of the physical game: the 22-square board and the 26-card
/// karma deck, exactly as documented in docs/GAME_RULES.md.
///
/// Plain C# with no Unity dependency so the rules can be unit tested.
/// </summary>
public static class StandardGame
{
    public const int BoardSize = 22;

    private const string ExpressText = "Caught Express Train – spin again and move that many extra spaces";

    /// <summary>Returns a fresh copy of the 22-square board, indexed clockwise from 0.</summary>
    public static BoardSquare[] CreateBoard()
    {
        return new BoardSquare[BoardSize]
        {
            // ── Top row: left → right (indices 0–5) ────────────────────────────────────
            new BoardSquare(0,  SquareType.Station,       SquarePolarity.Neutral,  "Broadway / 7 Avenue Local (1) — 96th Street",   "Draw a Karma Card"),
            new BoardSquare(1,  SquareType.EarlyTrain,    SquarePolarity.Positive, "Early Train — Move 1 Space Ahead",               "Move 1 space ahead"),
            new BoardSquare(2,  SquareType.NoOp,          SquarePolarity.Neutral,  "Empty",                                          ""),
            new BoardSquare(3,  SquareType.LateTrain,     SquarePolarity.Negative, "Late Train — Lose a Turn",                       "Lose a turn"),
            new BoardSquare(4,  SquareType.ExpressTrain,  SquarePolarity.Positive, "6 Avenue Express (F)",                           ExpressText),
            new BoardSquare(5,  SquareType.Station,       SquarePolarity.Neutral,  "6 Avenue Local / 47-50th Streets (F)",           "Draw a Karma Card"),
            // ── Right column: top → bottom (indices 6–10) ──────────────────────────────
            new BoardSquare(6,  SquareType.SickPassenger, SquarePolarity.Negative, "Sick Passenger — Go Back 1 Space",               "Go back 1 space"),
            new BoardSquare(7,  SquareType.NoOp,          SquarePolarity.Neutral,  "Empty",                                          ""),
            new BoardSquare(8,  SquareType.EarlyTrain,    SquarePolarity.Positive, "Early Train — Move 1 Space Ahead",               "Move 1 space ahead"),
            new BoardSquare(9,  SquareType.LateTrain,     SquarePolarity.Negative, "Late Train — Lose a Turn",                       "Lose a turn"),
            new BoardSquare(10, SquareType.ExpressTrain,  SquarePolarity.Positive, "8 Avenue Express (A)",                           ExpressText),
            // ── Bottom row: right → left (indices 11–16) ───────────────────────────────
            new BoardSquare(11, SquareType.Station,       SquarePolarity.Neutral,  "Times Square / Eighth Avenue Local (C)",         "Draw a Karma Card"),
            new BoardSquare(12, SquareType.EarlyTrain,    SquarePolarity.Positive, "Early Train — Move 1 Space Ahead",               "Move 1 space ahead"),
            new BoardSquare(13, SquareType.NoOp,          SquarePolarity.Neutral,  "Empty",                                          ""),
            new BoardSquare(14, SquareType.LateTrain,     SquarePolarity.Negative, "Late Train — Lose a Turn",                       "Lose a turn"),
            new BoardSquare(15, SquareType.ExpressTrain,  SquarePolarity.Positive, "Broadway Express (Q)",                           ExpressText),
            new BoardSquare(16, SquareType.Station,       SquarePolarity.Neutral,  "Astoria Blvd / Broadway Local (N)",              "Draw a Karma Card"),
            // ── Left column: bottom → top (indices 17–21) ──────────────────────────────
            new BoardSquare(17, SquareType.SickPassenger, SquarePolarity.Negative, "Sick Passenger — Go Back 1 Space",               "Go back 1 space"),
            new BoardSquare(18, SquareType.NoOp,          SquarePolarity.Neutral,  "Empty",                                          ""),
            new BoardSquare(19, SquareType.EarlyTrain,    SquarePolarity.Positive, "Early Train — Move 1 Space Ahead",               "Move 1 space ahead"),
            new BoardSquare(20, SquareType.LateTrain,     SquarePolarity.Negative, "Late Train — Lose a Turn",                       "Lose a turn"),
            new BoardSquare(21, SquareType.ExpressTrain,  SquarePolarity.Positive, "Seventh Avenue Express (2)",                     ExpressText),
        };
    }

    /// <summary>Returns the 26 karma cards in a fixed (unshuffled) order.</summary>
    public static List<KarmaCard> CreateDeck()
    {
        var deck = new List<KarmaCard>();

        // ── Positive cards (karma1.jpg) ─────────────────────────────────────
        deck.Add(new KarmaCard("Finds Metrocard",                   +5));
        deck.Add(new KarmaCard("Gets a Seat",                       +1));
        deck.Add(new KarmaCard("Holds Door for Someone",            +2));
        deck.Add(new KarmaCard("Gives Seat to Elderly",             +9));
        deck.Add(new KarmaCard("Gives Seat to Pregnant Woman",      +9));
        deck.Add(new KarmaCard("Seats Next to Celebrity",           +4));
        deck.Add(new KarmaCard("Finds Money",                       +5));
        deck.Add(new KarmaCard("Gives Money to Musician",           +2));
        deck.Add(new KarmaCard("Local Train Becomes Express",       +1));
        deck.Add(new KarmaCard("Finished Crossword",                +1));

        // ── Negative cards (karma2.jpg) ─────────────────────────────────────
        deck.Add(new KarmaCard("Loses Metrocard",                              -5));
        deck.Add(new KarmaCard("Overcrowded Train",                            -1));
        deck.Add(new KarmaCard("Fell Asleep, Got Robbed, and Woke Up Naked",  -9));
        deck.Add(new KarmaCard("Does Not Give Up Seat to Elderly",            -9));
        deck.Add(new KarmaCard("Rats!",                                        -1));
        deck.Add(new KarmaCard("Loses Wallet",                                -5));
        deck.Add(new KarmaCard("Smelly Subway Car",                           -6));
        deck.Add(new KarmaCard("Coffee Spills On You",                        -7));
        deck.Add(new KarmaCard("Miss Your Stop",                              -1));
        deck.Add(new KarmaCard("Does Not Give Up Seat to Pregnant Woman",     -9));

        // ── Special cards (karma3.jpg) — 3x Instant Karma, 3x Says Hello ───
        for (int i = 0; i < 3; i++)
            deck.Add(new KarmaCard("Instant Karma", 0, KarmaCardType.InstantKarma));
        for (int i = 0; i < 3; i++)
            deck.Add(new KarmaCard("Says Hello to Other Riders", 0, KarmaCardType.NoOp));

        return deck;
    }
}
