using System.Collections.Generic;

public abstract class InstructionData
{
}

public class MoveInstructionData : InstructionData
{
    public HexCoord Source;
    public HexCoord Destination;
}

public class OperationInstructionData : InstructionData
{
    public HexCoord Source;
    public HexCoord OperationTile;
    public OperationTileData.OperationType Operation;
    public List<HexCoord> AdditionalOperands = new List<HexCoord>();
}
