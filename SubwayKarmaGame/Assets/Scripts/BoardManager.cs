using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    public const int BoardSize = 22;

    private BoardSquare[] _squares;

    // Fired when a player lands on a square. Parameter is the square index.
    public UnityEvent<int> OnPlayerLands = new UnityEvent<int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeBoard();
    }

    private void InitializeBoard()
    {
        _squares = new BoardSquare[BoardSize]
        {
            // ── Top row: left → right (indices 0–5) ────────────────────────────────────
            new BoardSquare(0,  SquareType.Station,       SquarePolarity.Neutral,  "Broadway / 7 Avenue Local (1) — 96th Street",   "Draw a Karma Card"),
            new BoardSquare(1,  SquareType.EarlyTrain,    SquarePolarity.Positive, "Early Train — Move 1 Space Ahead",               "Move 1 space ahead"),
            new BoardSquare(2,  SquareType.NoOp,          SquarePolarity.Neutral,  "Empty",                                          ""),
            new BoardSquare(3,  SquareType.LateTrain,     SquarePolarity.Negative, "Late Train — Lose a Turn",                       "Lose a turn"),
            new BoardSquare(4,  SquareType.ExpressTrain,  SquarePolarity.Positive, "6 Avenue Express (F)",                           "Caught Express Train – spin again and move that many extra spaces"),
            new BoardSquare(5,  SquareType.Station,       SquarePolarity.Neutral,  "6 Avenue Local / 47-50th Streets (F)",           "Draw a Karma Card"),
            // ── Right column: top → bottom (indices 6–10) ──────────────────────────────
            new BoardSquare(6,  SquareType.SickPassenger, SquarePolarity.Negative, "Sick Passenger — Go Back 1 Space",               "Go back 1 space"),
            new BoardSquare(7,  SquareType.NoOp,          SquarePolarity.Neutral,  "Empty",                                          ""),
            new BoardSquare(8,  SquareType.EarlyTrain,    SquarePolarity.Positive, "Early Train — Move 1 Space Ahead",               "Move 1 space ahead"),
            new BoardSquare(9,  SquareType.LateTrain,     SquarePolarity.Negative, "Late Train — Lose a Turn",                       "Lose a turn"),
            new BoardSquare(10, SquareType.ExpressTrain,  SquarePolarity.Positive, "8 Avenue Express (A)",                           "Caught Express Train – spin again and move that many extra spaces"),
            // ── Bottom row: right → left (indices 11–16) ───────────────────────────────
            new BoardSquare(11, SquareType.Station,       SquarePolarity.Neutral,  "Times Square / Eighth Avenue Local (C)",         "Draw a Karma Card"),
            new BoardSquare(12, SquareType.EarlyTrain,    SquarePolarity.Positive, "Early Train — Move 1 Space Ahead",               "Move 1 space ahead"),
            new BoardSquare(13, SquareType.NoOp,          SquarePolarity.Neutral,  "Empty",                                          ""),
            new BoardSquare(14, SquareType.LateTrain,     SquarePolarity.Negative, "Late Train — Lose a Turn",                       "Lose a turn"),
            new BoardSquare(15, SquareType.ExpressTrain,  SquarePolarity.Positive, "Broadway Express (Q)",                           "Caught Express Train – spin again and move that many extra spaces"),
            new BoardSquare(16, SquareType.Station,       SquarePolarity.Neutral,  "Astoria Blvd / Broadway Local (N)",              "Draw a Karma Card"),
            // ── Left column: bottom → top (indices 17–21) ──────────────────────────────
            new BoardSquare(17, SquareType.SickPassenger, SquarePolarity.Negative, "Sick Passenger — Go Back 1 Space",               "Go back 1 space"),
            new BoardSquare(18, SquareType.NoOp,          SquarePolarity.Neutral,  "Empty",                                          ""),
            new BoardSquare(19, SquareType.EarlyTrain,    SquarePolarity.Positive, "Early Train — Move 1 Space Ahead",               "Move 1 space ahead"),
            new BoardSquare(20, SquareType.LateTrain,     SquarePolarity.Negative, "Late Train — Lose a Turn",                       "Lose a turn"),
            new BoardSquare(21, SquareType.ExpressTrain,  SquarePolarity.Positive, "Seventh Avenue Express (2)",                     "Caught Express Train – spin again and move that many extra spaces"),
        };
    }

    /// <summary>
    /// Returns the BoardSquare at the given index (wraps around the 20-square loop).
    /// </summary>
    public BoardSquare GetSquare(int index)
    {
        return _squares[index % BoardSize];
    }

    /// <summary>
    /// Returns the indices of every Station square a player passes through
    /// (including the landing square) for a move starting at fromIndex and
    /// advancing by the given number of steps clockwise.
    /// </summary>
    public List<int> GetStationsPassedThrough(int fromIndex, int steps)
    {
        var stations = new List<int>();
        for (int i = 1; i <= steps; i++)
        {
            int idx = (fromIndex + i) % BoardSize;
            if (_squares[idx].type == SquareType.Station)
                stations.Add(idx);
        }
        return stations;
    }
}
