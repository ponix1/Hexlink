using System.Collections.Generic;

// Program data model: one plain class per instruction the execution engine
// interprets. Deliberately free of visual/Unity dependencies.
public abstract class InstructionData
{
}

// Move the tile on Source to Destination.
public class MoveInstructionData : InstructionData
{
    public HexCoord Source;
    public HexCoord Destination;
}

// Spawn a palette-selected tile (the {5} spawn) at Source.
public class SelectInstructionData : InstructionData
{
    public HexCoord Source;
}

// Apply Operation (taken from the tile on OperationTile) to the tile on Source.
public class OperationInstructionData : InstructionData
{
    public HexCoord Source;
    public HexCoord OperationTile;
    public OperationTileData.OperationType Operation;
    // Extra operand hexes for multi-operand applications of the same operation.
    public List<HexCoord> AdditionalOperands = new List<HexCoord>();
}
