namespace Terranova.UI
{
    /// <summary>
    /// The build version shown in the corner of the main menu and the HUD.
    ///
    /// One constant instead of a literal per screen — the old UI had the string
    /// typed out in two places and they had already drifted apart (v0.5.8 in the
    /// menu, v0.5.10 in the HUD).
    /// </summary>
    public static class GameVersion
    {
        /// <summary>Current version, e.g. "v0.6.0".</summary>
        public const string Label = "v0.6.0";
    }
}
