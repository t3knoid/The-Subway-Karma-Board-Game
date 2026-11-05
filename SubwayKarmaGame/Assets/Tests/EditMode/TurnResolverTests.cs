using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Checks TurnResolver against docs/GAME_RULES.md using the real board.
/// Board reference: stations 0, 5, 11, 16 · Early 1, 8, 12, 19 · Late 3, 9, 14, 20 ·
/// Express 4, 10, 15, 21 · Sick 6, 17 · NoOp 2, 7, 13, 18.
/// </summary>
public class TurnResolverTests
{
    /// <summary>Deterministic deck: draws in list order.</summary>
    private sealed class ListDeck : ICardSource
    {
        private readonly Queue<KarmaCard> _cards;
        public ListDeck(int count)
        {
            _cards = new Queue<KarmaCard>();
            for (int i = 0; i < count; i++)
                _cards.Enqueue(new KarmaCard("Card " + i, i + 1));
        }
        public int Remaining { get { return _cards.Count; } }
        public KarmaCard Draw() { return _cards.Dequeue(); }
    }

    private ListDeck _deck;
    private TurnResolver _resolver;

    [SetUp]
    public void SetUp()
    {
        _deck = new ListDeck(26);
        _resolver = new TurnResolver(StandardGame.CreateBoard(), _deck);
    }

    private static int CardCount(TurnResult r) { return r.CardsDrawn.Count(); }

    // -- Standard game data ---------------------------------------------------

    [Test]
    public void StandardBoard_HasStationsAtCorners()
    {
        var stations = StandardGame.CreateBoard()
            .Where(s => s.type == SquareType.Station)
            .Select(s => s.squareIndex)
            .ToArray();
        CollectionAssert.AreEqual(new[] { 0, 5, 11, 16 }, stations);
    }

    [Test]
    public void StandardBoard_IndicesMatchPositions()
    {
        var board = StandardGame.CreateBoard();
        Assert.AreEqual(StandardGame.BoardSize, board.Length);
        for (int i = 0; i < board.Length; i++)
            Assert.AreEqual(i, board[i].squareIndex);
    }

    [Test]
    public void StandardDeck_Has10Positive10Negative6Special()
    {
        var deck = StandardGame.CreateDeck();
        Assert.AreEqual(26, deck.Count);
        Assert.AreEqual(10, deck.Count(c => c.points > 0));
        Assert.AreEqual(10, deck.Count(c => c.points < 0));
        Assert.AreEqual(6,  deck.Count(c => c.cardType != KarmaCardType.Standard && c.points == 0));
    }

    // -- Movement and stations ------------------------------------------------

    [Test]
    public void LandingOnStation_DrawsOneCard()
    {
        var r = _resolver.ResolveSpin(2, 3);   // 3, 4, 5
        Assert.AreEqual(5, r.Position);
        Assert.AreEqual(1, CardCount(r));
        Assert.AreEqual(25, _deck.Remaining);
    }

    [Test]
    public void PassingStation_DrawsCard_AndMoveIsSplitAtStation()
    {
        var r = _resolver.ResolveSpin(3, 4);   // 4, 5★, 6, 7 (NoOp)
        Assert.AreEqual(7, r.Position);
        Assert.AreEqual(1, CardCount(r));

        Assert.AreEqual(TurnEventKind.Moved,     r.Events[0].Kind);
        Assert.AreEqual(5,                       r.Events[0].To);
        Assert.AreEqual(2,                       r.Events[0].Steps);
        Assert.AreEqual(TurnEventKind.CardDrawn, r.Events[1].Kind);
        Assert.AreEqual(TurnEventKind.Moved,     r.Events[2].Kind);
        Assert.AreEqual(7,                       r.Events[2].To);
        Assert.AreEqual(2,                       r.Events[2].Steps);
        Assert.AreEqual(3, r.Events.Count);
    }

    [Test]
    public void MoveWrapsAroundBoard_AndDrawsAtStationZero()
    {
        var r = _resolver.ResolveSpin(18, 4);  // 19, 20, 21, 0★
        Assert.AreEqual(0, r.Position);
        Assert.AreEqual(1, CardCount(r));
    }

    [Test]
    public void LandingOnNoOp_DoesNothing()
    {
        var r = _resolver.ResolveSpin(0, 2);
        Assert.AreEqual(2, r.Position);
        Assert.AreEqual(0, CardCount(r));
        Assert.IsFalse(r.LoseNextTurn);
        Assert.IsFalse(r.NeedsBonusSpin);
        Assert.AreEqual(1, r.Events.Count);
    }

    // -- Square effects -------------------------------------------------------

    [Test]
    public void EarlyTrain_MovesOneAhead()
    {
        var r = _resolver.ResolveSpin(0, 1);   // 1 Early → 2 NoOp
        Assert.AreEqual(2, r.Position);
        Assert.AreEqual(0, CardCount(r));
    }

    [Test]
    public void EarlyTrain_AppliesEffectOfSquareReached()
    {
        var r = _resolver.ResolveSpin(7, 1);   // 8 Early → 9 Late
        Assert.AreEqual(9, r.Position);
        Assert.IsTrue(r.LoseNextTurn);
    }

    [Test]
    public void LateTrain_LosesNextTurn()
    {
        var r = _resolver.ResolveSpin(0, 3);
        Assert.AreEqual(3, r.Position);
        Assert.IsTrue(r.LoseNextTurn);
    }

    [Test]
    public void SickPassenger_GoesBackOne_WithoutDrawingAgain()
    {
        var r = _resolver.ResolveSpin(4, 2);   // 5★ (draw), 6 Sick → back to 5 (NoOp)
        Assert.AreEqual(5, r.Position);
        Assert.AreEqual(1, CardCount(r));
        var last = r.Events.Last();
        Assert.AreEqual(TurnEventKind.Moved, last.Kind);
        Assert.AreEqual(-1, last.Steps);
    }

    [Test]
    public void SickPassenger_BackOntoStation_IsNoOp()
    {
        var r = _resolver.ResolveSpin(5, 1);   // 6 Sick → 5★, no draw
        Assert.AreEqual(5, r.Position);
        Assert.AreEqual(0, CardCount(r));
    }

    [Test]
    public void ExpressTrain_RequiresBonusSpin()
    {
        var r = _resolver.ResolveSpin(0, 4);
        Assert.AreEqual(4, r.Position);
        Assert.IsTrue(r.NeedsBonusSpin);
    }

    [Test]
    public void BonusSpin_DrawsForStationCrossed_AndDoesNotChain()
    {
        var r = _resolver.ResolveBonusSpin(4, 6);  // 5★, 6, 7, 8, 9, 10 (Express)
        Assert.AreEqual(10, r.Position);
        Assert.AreEqual(1, CardCount(r));
        Assert.IsFalse(r.NeedsBonusSpin);
        Assert.IsFalse(r.LoseNextTurn);
    }

    [Test]
    public void BonusSpin_DoesNotApplyLandingSquare()
    {
        var r = _resolver.ResolveBonusSpin(4, 5);  // ends on 9 Late
        Assert.AreEqual(9, r.Position);
        Assert.IsFalse(r.LoseNextTurn);
    }

    // -- End of game ----------------------------------------------------------

    [Test]
    public void DrawingLastCard_EndsGameAndStopsMovement()
    {
        _deck = new ListDeck(1);
        _resolver = new TurnResolver(StandardGame.CreateBoard(), _deck);

        var r = _resolver.ResolveSpin(4, 2);   // would continue to 6 Sick, but deck runs out at 5
        Assert.IsTrue(r.DeckExhausted);
        Assert.AreEqual(5, r.Position);
        Assert.AreEqual(1, CardCount(r));
        Assert.AreEqual(TurnEventKind.DeckExhausted, r.Events.Last().Kind);
    }

    [Test]
    public void FullGame_DrawsEveryCardExactlyOnce()
    {
        var rng = new Random(1234);
        int position = 0, drawn = 0, turns = 0;
        bool over = false;

        while (!over)
        {
            Assert.Less(turns++, 10000, "Game never ended");
            var r = _resolver.ResolveSpin(position, rng.Next(1, 7));
            drawn += CardCount(r);
            over = r.DeckExhausted;
            position = r.Position;

            if (r.NeedsBonusSpin && !over)
            {
                var b = _resolver.ResolveBonusSpin(position, rng.Next(1, 7));
                drawn += CardCount(b);
                over = b.DeckExhausted;
                position = b.Position;
            }
        }

        Assert.AreEqual(26, drawn);
        Assert.AreEqual(0, _deck.Remaining);
    }

    // -- Validation -----------------------------------------------------------

    [Test]
    public void InvalidSpin_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _resolver.ResolveSpin(0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _resolver.ResolveSpin(0, 7));
    }

    [Test]
    public void InvalidPosition_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _resolver.ResolveSpin(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _resolver.ResolveSpin(22, 1));
    }
}
