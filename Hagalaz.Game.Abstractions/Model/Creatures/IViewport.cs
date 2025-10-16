using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Maps;

namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the contract for a creature's viewport, which manages the set of entities and map regions visible to the creature.
    /// </summary>
    public interface IViewport
    {
        /// <summary>
        /// Gets a read-only list of the creatures currently visible within this viewport.
        /// </summary>
        IReadOnlyList<ICreature> VisibleCreatures { get; }
        /// <summary>
        /// Gets a read-only list of the map regions currently loaded in this viewport.
        /// </summary>
        IReadOnlyList<IMapRegion> VisibleRegions { get; }
        /// <summary>
        /// Gets the central location around which the viewport is built.
        /// </summary>
        ILocation ViewLocation { get; }
        /// <summary>
        /// Gets the size of the viewport in tiles (e.g., 104x104).
        /// </summary>
        IMapSize MapSize { get; }
        /// <summary>
        /// Gets the absolute world X-coordinate of the bottom-left corner of the viewport.
        /// </summary>
        int BaseAbsX { get; }
        /// <summary>
        /// Gets the absolute world Y-coordinate of the bottom-left corner of the viewport.
        /// </summary>
        int BaseAbsY { get; }
        /// <summary>
        /// Determines if any of the currently visible map regions require dynamic updates (e.g., for animated tiles).
        /// </summary>
        /// <returns><c>true</c> if any visible region is dynamic; otherwise, <c>false</c>.</returns>
        bool NeedsDynamicDraw();
        /// <summary>
        /// Checks if a specific location is within the bounds of this viewport. This check does not consider the dimension.
        /// </summary>
        /// <param name="location">The location to check.</param>
        /// <returns><c>true</c> if the location is within the viewport's X and Y boundaries; otherwise, <c>false</c>.</returns>
        bool InBounds(ILocation location);
        /// <summary>
        /// Determines if the viewport needs to be completely rebuilt, typically because the creature has moved too far from the last build point.
        /// </summary>
        /// <returns><c>true</c> if a rebuild is recommended; otherwise, <c>false</c>.</returns>
        bool ShouldRebuild();
        /// <summary>
        /// Calculates the local coordinates of a world location relative to the viewport's origin.
        /// </summary>
        /// <param name="location">The world location.</param>
        /// <param name="localX">A reference parameter that will be populated with the local X-coordinate.</param>
        /// <param name="localY">A reference parameter that will be populated with the local Y-coordinate.</param>
        void GetLocalPosition(ILocation location, ref int localX, ref int localY);
        /// <summary>
        /// Processes a single game tick for the viewport, refreshing the list of visible creatures.
        /// </summary>
        void UpdateTick();
        /// <summary>
        /// Forces a complete rebuild of the viewport's visible map region data.
        /// </summary>
        void RebuildView();
        /// <summary>
        /// Asynchronously updates the viewport, loading new map regions and entities as needed.
        /// </summary>
        Task UpdateViewport();
    }
}
