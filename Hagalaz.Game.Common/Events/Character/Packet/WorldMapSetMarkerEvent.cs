using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character.Packet
{
    /// <summary>
    /// Class for world map set waypoint event.
    /// </summary>
    public class WorldMapSetMarkerEvent : CharacterEvent
    {
        /// <summary>
        /// Contains the x.
        /// </summary>
        public short X { get; private set; }

        /// <summary>
        /// Gets the y.
        /// </summary>
        public short Y { get; private set; }

        /// <summary>
        /// Gets the z.
        /// </summary>
        public byte Z { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldMapSetMarkerEvent" /> class.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="locationHash">The location hash.</param>
        public WorldMapSetMarkerEvent(ICharacter target, int locationHash) : base(target)
        {
            X = (short)((locationHash >> 14) & 0x3fff);
            Y = (short)(locationHash & 0x3fff);
            Z = (byte)(locationHash >> 28);
        }
    }
}