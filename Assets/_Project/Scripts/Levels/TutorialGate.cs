// Gameplay scripts call the *Allowed checks; TutorialRunner flips the flags per page
// (and per task, via the first-incomplete index). Lock state for the Level 1
// tutorial. While Active, the gated systems only allow what the current page
// permits: a set of palette tiles, tile placement vs authoring, the instruction
// hotkeys, the processor key, and Play.
using System.Collections.Generic;

public static class TutorialGate
{
    // Master switch: false (default) means everything is allowed.
    public static bool Active;

    // Normalized symbols allowed in the palette; empty = none.
    public static HashSet<string> AllowedTiles { get; } = new HashSet<string>();

    public static bool AllowPlacement;
    public static bool AllowAuthoring;
    public static bool AllowRemoval;
    public static bool AllowPlay;
    public static bool AllowSpaceKey;
    public static bool AllowOperationKey;
    public static bool AllowMoveKey;
    public static bool AllowCommitKey;
    public static bool AllowProcessorKey;

    public static bool TileAllowed(string symbol)
    {
        return !Active || AllowedTiles.Contains(symbol);
    }

    public static bool PlacementAllowed()
    {
        return !Active || AllowPlacement;
    }

    public static bool AuthoringAllowed()
    {
        return !Active || AllowAuthoring;
    }

    public static bool RemovalAllowed()
    {
        return !Active || AllowRemoval;
    }

    public static bool PlayAllowed()
    {
        return !Active || AllowPlay;
    }

    // Restore full freedom (tutorial finished or skipped).
    public static void UnlockAll()
    {
        Active = false;
        AllowedTiles.Clear();
        AllowPlacement = true;
        AllowAuthoring = true;
        AllowRemoval = true;
        AllowPlay = true;
        AllowSpaceKey = true;
        AllowOperationKey = true;
        AllowMoveKey = true;
        AllowCommitKey = true;
        AllowProcessorKey = true;
    }
}
