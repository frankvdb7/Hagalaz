using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Contains a creatures viewport
    /// </summary>
    public interface IViewport
    {
        /// <summary>
        /// Gets the visible creatures.
        /// </summary>
        /// <value>
        /// The visible creatures.
        /// </value>
        IReadOnlyList<ICreature> VisibleCreatures { get; }
        /// <summary>
        /// Get's creature surrounding regions + center region.
        /// </summary>
        /// <returns>LinkedList{MapRegion}.</returns>
        IReadOnlyList<IMapRegion> VisibleRegions { get; }
        /// <summary>
        /// Gets the view location.
        /// </summary>
        /// <value>
        /// The view location.
        /// </value>
        ILocation ViewLocation { get; }
        /// <summary>
        /// Contains size for both X and Y in tiles of this viewport.
        /// Let's say viewport size is 104 so the character could be able to see
        /// 104x104 tiles.
        /// </summary>
        /// <value>The size.</value>
        IMapSize MapSize { get; }
        /// <summary>
        /// Get's base X coordinate of this map.
        /// Base means starting absolute X.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int BaseAbsX { get; }
        /// <summary>
        /// Get's base Y coordinate of this map.
        /// Base means starting absolute Y.
        /// </summary>
        /// <returns>System.Int32.</returns>
        int BaseAbsY { get; }
        /// <summary>
        /// Get's if one of the visible region's are dynamic.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool NeedsDynamicDraw();
        /// <summary>
        /// Get's if specific location is in bounds of this game map.
        /// Dimension is not checked.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool InBounds(ILocation location);
        /// <summary>
        /// Get's if viewport is recommended to be updated.
        /// It checks if player sees black map part on it's minimap.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool ShouldRebuild();
        /// <summary>
        /// Gets the map position.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="localX">The local x.</param>
        /// <param name="localY">The local y.</param>
        void GetLocalPosition(ILocation location, ref int localX, ref int localY);
        /// <summary>
        /// Happens on update tick.
        /// Refreshe's visible characters.
        /// </summary>
        void UpdateTick();
        /// <summary>
        /// Rebuilds map region data.
        /// </summary>
        void RebuildView();
        /// <summary>
        /// Updates the map region data
        /// </summary>
        Task UpdateViewport();
    }
}
