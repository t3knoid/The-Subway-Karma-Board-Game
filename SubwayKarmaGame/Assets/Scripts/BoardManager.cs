using UnityEngine;
using UnityEngine.Events;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance { get; private set; }

    public const int BoardSize = StandardGame.BoardSize;

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
        _squares = StandardGame.CreateBoard();
    }

    /// <summary>The board squares in clockwise order (used by TurnResolver).</summary>
    public BoardSquare[] Squares => _squares;

    /// <summary>
    /// Returns the BoardSquare at the given index (wraps around the board loop).
    /// </summary>
    public BoardSquare GetSquare(int index)
    {
        return _squares[index % BoardSize];
    }
}
