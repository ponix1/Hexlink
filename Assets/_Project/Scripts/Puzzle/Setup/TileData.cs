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
    public enum OperationType { Add, Subtract, Multiply, Divide, Power, Factorial }
    public OperationType operation;

    public override string GetDisplayValue()
    {
        switch (operation)
        {
            case OperationType.Add: return "+";
            case OperationType.Subtract: return "-";
            case OperationType.Multiply: return "×";
            case OperationType.Divide: return "÷";
            case OperationType.Power: return "^";
            case OperationType.Factorial: return "!";
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

public static class TileDataFactory
{
    public static TileData CreateFromSymbol(string symbol)
    {
        // Numbers 0-9  
        if (int.TryParse(symbol, out int numberValue))
        {
            return new NumberTileData { value = numberValue };
        }

        // Operations
        switch (symbol)
        {
            case "+": return new OperationTileData { operation = OperationTileData.OperationType.Add };
            case "-": return new OperationTileData { operation = OperationTileData.OperationType.Subtract };
            case "*": return new OperationTileData { operation = OperationTileData.OperationType.Multiply };
            case "/": return new OperationTileData { operation = OperationTileData.OperationType.Divide };
            case "^": return new OperationTileData { operation = OperationTileData.OperationType.Power };
            case "!": return new OperationTileData { operation = OperationTileData.OperationType.Factorial };
        }

        // Final tile (target number defaults to 0 for now - revisit later)
        if (symbol == "_")
        {
            return new FinalTileData { targetNumber = 0 };
        }

        Debug.LogWarning($"TileDataFactory: unrecognized symbol '{symbol}'");
        return null;
    }
}