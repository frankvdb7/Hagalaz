namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    /// <summary>
    /// Defines the different rendering layers for game objects, which determine their draw order and clipping behavior.
    /// </summary>
    public enum LayerType : byte
    {
        /// <summary>
        /// The layer for solid walls and other major structures.
        /// </summary>
        Walls = 0,
        /// <summary>
        /// The layer for decorations attached to walls (e.g., paintings, torches).
        /// </summary>
        WallDecorations = 1,
        /// <summary>
        /// The layer for standard, interactive game objects (e.g., trees, anvils, chairs).
        /// </summary>
        StandardObjects = 2,
        /// <summary>
        /// The layer for decorations on the floor (e.g., rugs, rubble).
        /// </summary>
        FloorDecorations = 3
    }
}