using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Represents a single teleport info.
    /// </summary>
    public readonly struct Teleport : ITeleport
    {
        /// <summary>
        /// Contains teleport location.
        /// </summary>
        /// <value>The location.</value>
        public ILocation Location { get; }

        /// <summary>
        /// Contains the teleport type
        /// </summary>
        public MovementType Type { get; }

        /// <summary>
        /// Constructs a new teleport.
        /// </summary>
        /// <param name="loc">The loc.</param>
        /// <param name="type"></param>
        private Teleport(ILocation loc, MovementType type)
        {
            Location = loc;
            Type = type;
        }

        /// <summary>
        /// Creates a new teleport.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>Returns a new teleport containing its data.</returns>
        public static Teleport Create(ILocation location) => new(location, MovementType.Warp);
        /// <summary>
        /// Creates a new teleport at the specified location and type.
        /// </summary>
        /// <param name="location"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Teleport Create(ILocation location, MovementType type) => new(location, type);
    }
}