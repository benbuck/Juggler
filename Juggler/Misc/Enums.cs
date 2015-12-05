namespace Juggler
{
    /// <summary>
    /// Style enum for setting wallpaper.
    /// </summary>
    internal enum Style : int
    {
        FitToScreen = 0,
        BestFit = 1,
        Tiled = 2,
        Centered = 3
    }

    /// <summary>
    /// Stores the menu indexes in enum to avoid collection traversing while accessing items and removing string literals
    /// </summary>
    internal enum MenuIndex : int
    {
        LastWallpaper,
        JuggleNow,
        Delete,
        Active,
        Preferences,
        Exit
    }
}
