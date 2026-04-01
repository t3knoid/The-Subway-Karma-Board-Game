using UnityEngine;

public enum SquareType
{
    Station,
    ExpressTrain,
    EarlyTrain,
    LateTrain,
    SickPassenger
}

public enum SquarePolarity
{
    Positive,
    Negative,
    Neutral
}

[System.Serializable]
public class BoardSquare
{
    public int squareIndex;
    public SquareType type;
    public SquarePolarity polarity;
    public string displayName;
    public string instructionText;

    public BoardSquare(int index, SquareType type, SquarePolarity polarity, string displayName, string instructionText)
    {
        this.squareIndex = index;
        this.type = type;
        this.polarity = polarity;
        this.displayName = displayName;
        this.instructionText = instructionText;
    }
}
