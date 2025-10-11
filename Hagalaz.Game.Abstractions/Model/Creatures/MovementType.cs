namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the different types of movement a creature can perform.
    /// </summary>
    public enum MovementType
    {
        /// <summary>
        /// Standard walking movement.
        /// </summary>
        Walk = 1,
        /// <summary>
        /// Standard running movement.
        /// </summary>
        Run = 2,
        /// <summary>
        /// Walking backwards, typically for specific animations or effects.
        /// </summary>
        WalkingBackwards = 3,
        /// <summary>
        /// An instantaneous teleport or "warp" to a new location.
        /// </summary>
        Warp = 127,
    }
}
