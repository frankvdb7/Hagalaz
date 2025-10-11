namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Defines the direction or position of a hint icon relative to its target location.
    /// </summary>
    public enum HintIconDirection
    {
        /// <summary>
        /// The hint icon is positioned directly in the center of the target tile.
        /// </summary>
        Center,
        /// <summary>
        /// The hint icon is positioned on the western edge of the target tile.
        /// </summary>
        West,
        /// <summary>
        /// The hint icon is positioned on the eastern edge of the target tile.
        /// </summary>
        East,
        /// <summary>
        /// The hint icon is positioned on the southern edge of the target tile.
        /// </summary>
        South,
        /// <summary>
        /// The hint icon is positioned on the northern edge of the target tile.
        /// </summary>
        North
    }
}