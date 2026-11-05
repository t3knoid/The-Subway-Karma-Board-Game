using System;
using System.Collections.Generic;

/// <summary>Anything cards can be drawn from (the Unity KarmaDeck, or a test deck).</summary>
public interface ICardSource
{
    int Remaining { get; }

    /// <summary>Removes and returns the top card. Only called when Remaining &gt; 0.</summary>
    KarmaCard Draw();
}

public enum TurnEventKind
{
    /// <summary>Token moved Steps squares (negative = backwards) from From to To.</summary>
    Moved,
    /// <summary>Card was drawn at the station the token is now on.</summary>
    CardDrawn,
    /// <summary>Player landed on Late Train and must skip their next turn.</summary>
    LoseNextTurn,
    /// <summary>Player landed on Express Train and must spin again (call ResolveBonusSpin).</summary>
    BonusSpinRequired,
    /// <summary>The last card was drawn; the game is over.</summary>
    DeckExhausted
}

/// <summary>One thing that happened during a turn, in the order it happened.</summary>
public sealed class TurnEvent
{
    public TurnEventKind Kind  { get; private set; }
    public int           From  { get; private set; }
    public int           To    { get; private set; }
    public int           Steps { get; private set; }
    public KarmaCard     Card  { get; private set; }

    private TurnEvent() { }

    public static TurnEvent Moved(int from, int to, int steps)
    {
        return new TurnEvent { Kind = TurnEventKind.Moved, From = from, To = to, Steps = steps };
    }

    public static TurnEvent At(TurnEventKind kind, int position, KarmaCard card = null)
    {
        return new TurnEvent { Kind = kind, From = position, To = position, Card = card };
    }

    public override string ToString()
    {
        return Kind == TurnEventKind.Moved
            ? string.Format("Moved {0} -> {1} ({2})", From, To, Steps)
            : string.Format("{0} @ {1}{2}", Kind, To, Card != null ? " " + Card : "");
    }
}

/// <summary>Outcome of resolving one spin.</summary>
public sealed class TurnResult
{
    public readonly List<TurnEvent> Events = new List<TurnEvent>();

    /// <summary>Where the token ends up.</summary>
    public int  Position;
    public bool LoseNextTurn;
    public bool NeedsBonusSpin;
    public bool DeckExhausted;

    public IEnumerable<KarmaCard> CardsDrawn
    {
        get
        {
            foreach (var e in Events)
                if (e.Kind == TurnEventKind.CardDrawn) yield return e.Card;
        }
    }
}

/// <summary>
/// The rules of The Subway Karma Game (see docs/GAME_RULES.md), independent of
/// Unity. Given a position and a spin, it works out every move, card draw and
/// penalty, and returns them as an ordered list of events for the presentation
/// layer to animate. It never touches player state itself.
///
/// A turn is:
///   ResolveSpin(position, spin)
///   if result.NeedsBonusSpin: ResolveBonusSpin(result.Position, secondSpin)
/// </summary>
public sealed class TurnResolver
{
    public const int MinSpin = 1;
    public const int MaxSpin = 6;

    private readonly BoardSquare[] _board;
    private readonly ICardSource   _deck;

    public TurnResolver(BoardSquare[] board, ICardSource deck)
    {
        if (board == null || board.Length == 0) throw new ArgumentException("Board must have squares.", "board");
        if (deck == null) throw new ArgumentNullException("deck");
        _board = board;
        _deck  = deck;
    }

    public int BoardSize { get { return _board.Length; } }

    /// <summary>
    /// Resolves the normal spin at the start of a turn: move forward, draw a card
    /// for every station passed or landed on, then apply the landing square.
    /// </summary>
    public TurnResult ResolveSpin(int position, int spin)
    {
        var result = Begin(position, spin);
        MoveForward(result, spin);
        if (!result.DeckExhausted)
            ApplyLandingSquare(result, true);
        return result;
    }

    /// <summary>
    /// Resolves the extra spin from an Express Train: move forward, drawing for
    /// stations passed or landed on. The square reached is not otherwise applied,
    /// so Express Trains do not chain.
    /// </summary>
    public TurnResult ResolveBonusSpin(int position, int spin)
    {
        var result = Begin(position, spin);
        MoveForward(result, spin);
        return result;
    }

    // -- Internals ------------------------------------------------------------

    private TurnResult Begin(int position, int spin)
    {
        if (position < 0 || position >= _board.Length)
            throw new ArgumentOutOfRangeException("position", position, "Not a square on the board.");
        if (spin < MinSpin || spin > MaxSpin)
            throw new ArgumentOutOfRangeException("spin", spin, "Spin must be 1-6.");
        return new TurnResult { Position = position };
    }

    private int Wrap(int index)
    {
        int n = _board.Length;
        return ((index % n) + n) % n;
    }

    /// <summary>
    /// Moves forward, splitting the move at each station so the card is drawn
    /// as the token reaches it. Stops early if the deck runs out.
    /// </summary>
    private void MoveForward(TurnResult result, int steps)
    {
        int remaining = steps;
        while (remaining > 0 && !result.DeckExhausted)
        {
            int from    = result.Position;
            int pos     = from;
            int taken   = 0;
            bool station = false;

            while (taken < remaining)
            {
                taken++;
                pos = Wrap(pos + 1);
                if (_board[pos].type == SquareType.Station) { station = true; break; }
            }

            result.Events.Add(TurnEvent.Moved(from, pos, taken));
            result.Position = pos;
            remaining -= taken;

            if (station)
                DrawCard(result);
        }
    }

    private void DrawCard(TurnResult result)
    {
        if (_deck.Remaining > 0)
        {
            var card = _deck.Draw();
            result.Events.Add(TurnEvent.At(TurnEventKind.CardDrawn, result.Position, card));
        }

        if (_deck.Remaining == 0)
        {
            result.DeckExhausted = true;
            result.Events.Add(TurnEvent.At(TurnEventKind.DeckExhausted, result.Position));
        }
    }

    /// <param name="allowEarlyTrain">
    /// False after an Early Train hop, so a second Early Train would not chain.
    /// (The standard board never puts two in a row.)
    /// </param>
    private void ApplyLandingSquare(TurnResult result, bool allowEarlyTrain)
    {
        switch (_board[result.Position].type)
        {
            case SquareType.ExpressTrain:
                result.NeedsBonusSpin = true;
                result.Events.Add(TurnEvent.At(TurnEventKind.BonusSpinRequired, result.Position));
                break;

            case SquareType.EarlyTrain:
                if (!allowEarlyTrain) break;
                MoveForward(result, 1);
                if (!result.DeckExhausted)
                    ApplyLandingSquare(result, false);
                break;

            case SquareType.LateTrain:
                result.LoseNextTurn = true;
                result.Events.Add(TurnEvent.At(TurnEventKind.LoseNextTurn, result.Position));
                break;

            case SquareType.SickPassenger:
            {
                // Moving back never draws a card, and the square reached is a NoOp.
                int from = result.Position;
                result.Position = Wrap(from - 1);
                result.Events.Add(TurnEvent.Moved(from, result.Position, -1));
                break;
            }

            case SquareType.Station:   // card already drawn on arrival
            case SquareType.NoOp:
            default:
                break;
        }
    }
}
