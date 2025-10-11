namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the contract for a teleport request, specifying the destination and movement type.
    /// </summary>
    public interface ITeleport
    {
        /// <summary>
        /// Gets the destination location for the teleport.
        /// </summary>
        ILocation Location { get; }

        /// <summary>
        /// Gets the type of teleport, which can affect visual effects or movement restrictions.
        /// </summary>
        MovementType Type { get; }
    }
}
