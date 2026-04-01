/// <summary>
/// Type of Karma card.
///   Standard    — normal positive or negative point card.
///   InstantKarma — "Instant Karma: Take a random card from another player."
///                  No-op in solitaire; display card, show "No effect in solo play".
///   NoOp        — "Says Hello to Other Riders." Always 0 pts; display and end turn.
/// </summary>
public enum KarmaCardType
{
    Standard,
    InstantKarma,
    NoOp
}

/// <summary>
/// Immutable data for a single Karma card.
/// </summary>
[System.Serializable]
public class KarmaCard
{
    public readonly string       title;
    public readonly int          points;
    public readonly KarmaCardType cardType;

    public KarmaCard(string title, int points, KarmaCardType cardType = KarmaCardType.Standard)
    {
        this.title    = title;
        this.points   = points;
        this.cardType = cardType;
    }

    public override string ToString() =>
        $"[{cardType}] {title} ({(points >= 0 ? "+" : "")}{points})";
}
