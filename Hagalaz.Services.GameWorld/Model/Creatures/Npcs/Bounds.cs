using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Npcs
{
    /// <summary>
    /// Class for npc bounds.
    /// </summary>
    public class Bounds : IBounds
    {
        /// <summary>
        /// The NPC's default spawn location.
        /// </summary>
        /// <value>The default location.</value>
        public ILocation DefaultLocation { get; }

        /// <summary>
        /// The NPC's minimum location.
        /// </summary>
        /// <value>The minimum location.</value>
        public ILocation MinimumLocation { get; }

        /// <summary>
        /// The NPC's maximum location.
        /// </summary>
        /// <value>The maximum location.</value>
        public ILocation MaximumLocation { get; }

        /// <summary>
        /// Contains npc bounds type.
        /// </summary>
        /// <value>The type of the bounds.</value>
        public BoundsType BoundsType { get; }

        /// <summary>
        /// Create's new bounds class.
        /// </summary>
        /// <param name="boundsType">Type of the bounds.</param>
        /// <param name="defaultLocation">The default location.</param>
        /// <param name="minimum">The minimum.</param>
        /// <param name="maximum">The maximum.</param>
        public Bounds(BoundsType boundsType, ILocation defaultLocation, ILocation minimum, ILocation maximum)
        {
            DefaultLocation = defaultLocation;
            MinimumLocation = minimum;
            MaximumLocation = maximum;
            BoundsType = boundsType;
        }

        /// <summary>
        /// Get's if specific location is in npc location bounds.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool InBounds(ILocation location)
        {
            if (BoundsType == BoundsType.Roam)
                return true;
            else if (BoundsType == BoundsType.Static)
                return false;

            if (location.Dimension < MinimumLocation.Dimension || location.Dimension > MaximumLocation.Dimension)
                return false;
            if (location.Z < MinimumLocation.Z || location.Z > MaximumLocation.Z)
                return false;
            if (location.X < MinimumLocation.X || location.X > MaximumLocation.X)
                return false;
            if (location.Y < MinimumLocation.Y || location.Y > MaximumLocation.Y)
                return false;
            return true;
        }
    }
}