using UnityEngine;

[System.Serializable]
public abstract class TileData
{
    public abstract string GetDisplayValue();
}

[System.Serializable]
public class NumberTileData : TileData
{
    public int value;

    public override string GetDisplayValue()
    {
        return value.ToString();
    }
}

[System.Serializable]
public class OperationTileData : TileData
{
    public enum OperationType { Add, Subtract, Multiply, Divide }
    public OperationType operation;

    public override string GetDisplayValue()
    {
        switch (operation)
        {
            case OperationType.Add: return "+";
            case OperationType.Subtract: return "-";
            case OperationType.Multiply: return "×";
            case OperationType.Divide: return "÷";
            default: return "?";
        }
    }
}

[System.Serializable]
public class FinalTileData : TileData
{
    public int targetNumber;

    public override string GetDisplayValue()
    {
        return "Target: " + targetNumber;
    }
}