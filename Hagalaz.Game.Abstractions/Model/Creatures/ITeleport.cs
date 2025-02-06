namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITeleport
    {
        /// <summary>
        /// Contains teleport location.
        /// </summary>
        /// <value>The location.</value>
        ILocation Location { get; }
        /// <summary>
        /// Contains teleport type.
        /// By default, this is the warp movement type.
        /// </summary>
        /// <value>The type.</value>
        MovementType Type { get; }
    }
}
