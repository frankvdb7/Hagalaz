using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    public class Viewport : IViewport
    {
        private IMapSize _mapSize;
        private readonly ICreature _owner;
        private readonly IMapRegionService _regionService;
        private readonly List<IMapRegion> _visibleRegions = [];
        private readonly ListHashSet<ICreature> _visibleCreatures = new();

        /// <summary>
        /// Contains size for both X and Y in tiles of this viewport.
        /// Let's say viewport size is 104 so the character could be able to see
        /// 104x104 tiles.
        /// Set's viewport size,
        /// after this method is called, call to RebuildMap() is a must.
        /// </summary>
        /// <value>
        /// The size.
        /// </value>
        /// <exception cref="NotSupportedException">Map size: " + value + " is not supported by client!</exception>
        /// <exception cref="System.NotSupportedException"></exception>
        public IMapSize MapSize
        {
            get => _mapSize;
            set
            {
                if (_mapSize.Type != value.Type)
                {
                    _mapSize = value;
                    RebuildView();
                }
            }
        }

        /// <summary>
        /// Contains ViewLocation from previous map update,
        /// can be null.
        /// </summary>
        /// <value>The previous view location.</value>
        public ILocation? PreviousViewLocation { get; private set; }

        /// <summary>
        /// Contains Minimum bounds location from previous update.
        /// Can be null.
        /// </summary>
        /// <value>The previous bounds minimum.</value>
        public ILocation? PreviousBoundsMinimum { get; private set; }

        /// <summary>
        /// Contains Maximum bounds location from previous update.
        /// Can be null.
        /// </summary>
        /// <value>The previous bounds maximum.</value>
        public ILocation? PreviousBoundsMaximum { get; private set; }

        /// <summary>
        /// Get's location from which this viewport was created.
        /// centerRegion Id is equal to ViewLocation region Id.
        /// </summary>
        /// <value>The view location.</value>
        public ILocation ViewLocation { get; private set; } = Location.Zero;

        /// <summary>
        /// Get's bounds minimum location.
        /// </summary>
        /// <value>The bounds minimum.</value>
        public ILocation BoundsMinimum { get; private set; } = Location.Zero;

        /// <summary>
        /// Get's bounds maximum location.
        /// </summary>
        /// <value>The bounds maximum.</value>
        public ILocation BoundsMaximum { get; private set; } = Location.Zero;

        /// <summary>
        /// Get's list of visible creature's.
        /// </summary>
        /// <returns>List{Creature}.</returns>
        public IReadOnlyList<ICreature> VisibleCreatures => _visibleCreatures;

        /// <summary>
        /// Get's creature surrounding regions + center region.
        /// </summary>
        /// <returns>LinkedList{MapRegion}.</returns>
        public IReadOnlyList<IMapRegion> VisibleRegions => _visibleRegions;

        /// <summary>
        /// Get's previous map base X , can be -1 if previous
        /// location is null.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int PreviousBaseX
        {
            get
            {
                if (PreviousViewLocation != null)
                    return (PreviousViewLocation.RegionPartX - (MapSize.Size >> 4)) * 8;
                return -1;
            }
        }

        /// <summary>
        /// Get's previous map base Y , can be -1 if previous
        /// location is null.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int PreviousBaseY
        {
            get
            {
                if (PreviousViewLocation != null)
                    return (PreviousViewLocation.RegionPartY - (MapSize.Size >> 4)) * 8;
                return -1;
            }
        }

        /// <summary>
        /// Get's base X coordinate of this map.
        /// Base means starting absolute X.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int BaseAbsX => (ViewLocation.RegionPartX - (MapSize.Size >> 4)) * 8;

        /// <summary>
        /// Get's base Y coordinate of this map.
        /// Base means starting absolute Y.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int BaseAbsY => (ViewLocation.RegionPartY - (MapSize.Size >> 4)) * 8;

        public Viewport(ICreature owner, IMapRegionService regionService, IMapSize mapSize)
        {
            _owner = owner;
            _regionService = regionService;
            _mapSize = mapSize;
        }

        /// <summary>
        /// Happens on update tick.
        /// Refreshe's visible creatures.
        /// </summary>
        public void UpdateTick()
        {
            _visibleCreatures.Clear();

            foreach (var region in _visibleRegions)
            {
                foreach (var character in region.FindAllCharacters()
                    .Where(character =>
                        InBounds(character.Location) &&
                        _owner.Location.WithinDistance(character.Location, CreatureConstants.VisibilityDistance) &&
                        character.Appearance.Visible))
                {
                    _visibleCreatures.Add(character);
                }
                foreach (var npc in region.FindAllNpcs()
                    .Where(npc =>
                    InBounds(npc.Location) &&
                    _owner.Location.WithinDistance(npc.Location, CreatureConstants.VisibilityDistance) &&
                    npc.Appearance.Visible))
                {
                    _visibleCreatures.Add(npc);
                }
            }
        }

        /// <summary>
        /// Rebuilds region data.
        /// </summary>
        public void RebuildView()
        {
            _visibleRegions.Clear();
            PreviousViewLocation = ViewLocation;
            ViewLocation = _owner.Location.Clone();
            PreviousBoundsMinimum = BoundsMinimum;
            BoundsMinimum = new Location(BaseAbsX, BaseAbsY, 0, ViewLocation.Dimension);
            PreviousBoundsMaximum = BoundsMaximum;
            BoundsMaximum = new Location(BaseAbsX + (MapSize.Size - 1), BaseAbsY + (MapSize.Size - 1), 3, ViewLocation.Dimension);

            _visibleRegions.AddRange(_regionService.GetMapRegionsWithinRange(ViewLocation, true, true, MapSize));
        }

        public async Task UpdateViewport()
        {
            if (_owner is not ICharacter character)
            {
                return;
            }
            foreach (var region in _visibleRegions)
            {
                await _regionService.LoadRegionAsync(region);
                region.SendFullPartUpdates(character);
            }
        }

        /// <summary>
        /// Get's if one of the visible region's are dynamic.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool NeedsDynamicDraw() => _visibleRegions.Any(r => r.IsDynamic);

        /// <summary>
        /// Get's if viewport is recommended to be updated.
        /// It checks if player sees black map part on it's minimap.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool ShouldRebuild()
        {
            if (ViewLocation.Dimension != _owner.Location.Dimension)
                return true;

            var diffX = Math.Abs(ViewLocation.RegionPartX - _owner.Location.RegionPartX);
            var diffY = Math.Abs(ViewLocation.RegionPartY - _owner.Location.RegionPartY);
            var size = ((MapSize.Size >> 3) / 2) - 1;
            return diffX >= size || diffY >= size;
        }

        /// <summary>
        /// Get's if specific location is in bounds of this game map.
        /// Dimension is not checked.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool InBounds(ILocation location) =>
            location.X >= BoundsMinimum.X && location.X <= BoundsMaximum.X
                                          && location.Y >= BoundsMinimum.Y && location.Y <= BoundsMaximum.Y
                                          && location.Z >= BoundsMinimum.Z && location.Z <= BoundsMaximum.Z;

        /// <summary>
        /// Get's if specific entity is in bounds of previous game map.
        /// Dimension is not checked.
        /// This method is equal to InPreviousMapBounds(entity.Location);
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool InPreviousMapBounds(IEntity entity) => InPreviousMapBounds(entity.Location);

        /// <summary>
        /// Get's if specific location is in bounds of previous game map.
        /// Dimension is not checked.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool InPreviousMapBounds(ILocation location)
        {
            if (PreviousBoundsMinimum == null || PreviousBoundsMaximum == null)
                return false;
            return location.X >= PreviousBoundsMinimum.X && location.X <= PreviousBoundsMaximum.X
                                                         && location.Y >= PreviousBoundsMinimum.Y && location.Y <= PreviousBoundsMaximum.Y
                                                         && location.Z >= PreviousBoundsMinimum.Z && location.Z <= PreviousBoundsMaximum.Z;
        }

        /// <summary>
        /// Get's map position of specific location in previous map.
        /// When method return's the addresses refered in parameters (mapX and mapY)
        /// will be filled with the position of specific location.
        /// Filled position will be invalid if location is not in map bounds.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="mapX">The map X.</param>
        /// <param name="mapY">The map Y.</param>
        public void GetPreviousLocalPosition(ILocation location, ref int mapX, ref int mapY)
        {
            mapX = location.X - PreviousBaseX;
            mapY = location.Y - PreviousBaseY;
        }

        /// <summary>
        /// Get's map position of specific location.
        /// When method return's the addresses refered in parameters (mapX and mapY)
        /// will be filled with the position of specific location.
        /// Filled position will be invalid if location is not in map bounds.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="mapX">The map X.</param>
        /// <param name="mapY">The map Y.</param>
        public void GetLocalPosition(ILocation location, ref int mapX, ref int mapY)
        {
            mapX = location.X - BaseAbsX;
            mapY = location.Y - BaseAbsY;
        }
    }
}