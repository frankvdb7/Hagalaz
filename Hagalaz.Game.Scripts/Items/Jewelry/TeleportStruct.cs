using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Items.Jewelry
{
    /// <summary>
    /// </summary>
    public struct TeleportStruct
    {
        /// <summary>
        ///     The location.
        /// </summary>
        public ILocation Location;

        /// <summary>
        ///     The teleport distance.
        /// </summary>
        public int TeleportDistance;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TeleportStruct" /> struct.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="teleportDistance">The teleport distance.</param>
        public TeleportStruct(ILocation location, int teleportDistance)
        {
            Location = location;
            TeleportDistance = teleportDistance;
        }
    }
}