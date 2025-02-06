namespace Hagalaz.Services.GameWorld.Logic.Pathfinding
{
    /// <summary>
    /// 
    /// </summary>
    public enum Interaction
    {
        /// <summary>
        /// Can't currently interact.
        /// </summary>
        None,

        /// <summary>
        /// Can currently interact and doesn't have to move.
        /// </summary>
        Still,

        /// <summary>
        /// Can interact but has to move.
        /// </summary>
        Moving
    }
}