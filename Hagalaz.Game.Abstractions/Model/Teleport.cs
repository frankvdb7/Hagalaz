using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// Represents a request to teleport a creature to a new location.
    /// </summary>
    public readonly struct Teleport : ITeleport
    {
        /// <summary>
        /// Gets the destination location for the teleport.
        /// </summary>
        public ILocation Location { get; }

        /// <summary>
        /// Gets the type of teleport, which can affect visual effects or movement restrictions.
        /// </summary>
        public MovementType Type { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Teleport"/> struct.
        /// </summary>
        /// <param name="loc">The destination location.</param>
        /// <param name="type">The type of teleport.</param>
        private Teleport(ILocation loc, MovementType type)
        {
            Location = loc;
            Type = type;
        }

        /// <summary>
        /// Creates a new teleport request with a default movement type of <see cref="MovementType.Warp"/>.
        /// </summary>
        /// <param name="location">The destination location.</param>
        /// <returns>A new <see cref="Teleport"/> instance.</returns>
        public static Teleport Create(ILocation location) => new(location, MovementType.Warp);

        /// <summary>
        /// Creates a new teleport request with a specified movement type.
        /// </summary>
        /// <param name="location">The destination location.</param>
        /// <param name="type">The type of teleport movement.</param>
        /// <returns>A new <see cref="Teleport"/> instance.</returns>
        public static Teleport Create(ILocation location, MovementType type) => new(location, type);
    }
}