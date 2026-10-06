// Gameplay scripts call the *Allowed checks; TutorialRunner flips the flags per step.
// Lock state for the Level 1 tutorial. While Active, the gated systems only
// allow what the current step permits: one palette tile at a time, tile
// placement vs authoring, the instruction hotkeys, and Play.
public static class TutorialGate
{
    // Master switch: false (default) means everything is allowed.
    public static bool Active;

    public static string AllowedTile;        // normalized symbol; null = none
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
        return !Active || symbol == AllowedTile;
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
        AllowedTile = null;
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
