using System.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    /// <summary>
    /// Represents a creature's viewport, which manages visible entities and regions.
    /// </summary>
    public class Viewport : IViewport
    {
        private IMapSize _mapSize;
        private readonly ICreature _owner;
        private readonly IMapRegionService _regionService;
        private readonly List<IMapRegion> _visibleRegions = [];
        private readonly ListHashSet<ICreature> _visibleCreatures = new(255);
        private readonly ListHashSet<ICharacter> _visibleCharacters = new(255);
        private readonly ListHashSet<INpc> _visibleNpcs = new(255);

        /// <summary>
        /// Contains size for both X and Y in tiles of this viewport.
        /// </summary>
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
        /// Contains ViewLocation from previous map update, can be null.
        /// </summary>
        public ILocation? PreviousViewLocation { get; private set; }

        /// <summary>
        /// Contains Minimum bounds location from previous update.
        /// </summary>
        public ILocation? PreviousBoundsMinimum { get; private set; }

        /// <summary>
        /// Contains Maximum bounds location from previous update.
        /// </summary>
        public ILocation? PreviousBoundsMaximum { get; private set; }

        /// <summary>
        /// Get's location from which this viewport was created.
        /// </summary>
        public ILocation ViewLocation { get; private set; } = Location.Zero;

        /// <summary>
        /// Get's bounds minimum location.
        /// </summary>
        public ILocation BoundsMinimum { get; private set; } = Location.Zero;

        /// <summary>
        /// Get's bounds maximum location.
        /// </summary>
        public ILocation BoundsMaximum { get; private set; } = Location.Zero;

        /// <summary>
        /// Get's list of visible creature's.
        /// </summary>
        public IReadOnlyList<ICreature> VisibleCreatures => _visibleCreatures;

        /// <summary>
        /// Gets list of visible characters.
        /// </summary>
        public IReadOnlyCollection<ICharacter> VisibleCharacters => _visibleCharacters;

        /// <summary>
        /// Gets list of visible npcs.
        /// </summary>
        public IReadOnlyCollection<INpc> VisibleNpcs => _visibleNpcs;

        /// <summary>
        /// Get's creature surrounding regions + center region.
        /// </summary>
        public IReadOnlyList<IMapRegion> VisibleRegions => _visibleRegions;

        /// <summary>
        /// Get's previous map base X.
        /// </summary>
        public int PreviousBaseX => PreviousViewLocation != null ? (PreviousViewLocation.RegionPartX - (MapSize.Size >> 4)) * 8 : -1;

        /// <summary>
        /// Get's previous map base Y.
        /// </summary>
        public int PreviousBaseY => PreviousViewLocation != null ? (PreviousViewLocation.RegionPartY - (MapSize.Size >> 4)) * 8 : -1;

        /// <summary>
        /// Get's base X coordinate of this map.
        /// </summary>
        public int BaseAbsX => (ViewLocation.RegionPartX - (MapSize.Size >> 4)) * 8;

        /// <summary>
        /// Get's base Y coordinate of this map.
        /// </summary>
        public int BaseAbsY => (ViewLocation.RegionPartY - (MapSize.Size >> 4)) * 8;

        public Viewport(ICreature owner, IMapRegionService regionService, IMapSize mapSize)
        {
            _owner = owner;
            _regionService = regionService;
            _mapSize = mapSize;
        }

        /// <summary>
        /// Happens on update tick. Refreshes visible creatures.
        /// </summary>
        public void UpdateTick()
        {
            _visibleCreatures.Clear();
            _visibleCharacters.Clear();
            _visibleNpcs.Clear();

            var state = new ViewportUpdateState(this, _owner.Location);
            foreach (var region in _visibleRegions)
            {
                region.ForEachCharacter((c, s) => s.Viewport.ProcessCharacter(c, s.OwnerLocation), state);
                region.ForEachNpc((n, s) => s.Viewport.ProcessNpc(n, s.OwnerLocation), state);
            }
        }

        /// <summary>
        /// Processes a character to check if it should be added to the viewport.
        /// </summary>
        protected virtual void ProcessCharacter(ICharacter character, ILocation ownerLocation)
        {
            if (InBounds(character.Location) &&
                ownerLocation.WithinDistance(character.Location, CreatureConstants.VisibilityDistance) &&
                character.Appearance.Visible)
            {
                _visibleCreatures.Add(character);
                _visibleCharacters.Add(character);
            }
        }

        /// <summary>
        /// Processes an NPC to check if it should be added to the viewport.
        /// </summary>
        protected virtual void ProcessNpc(INpc npc, ILocation ownerLocation)
        {
            if (InBounds(npc.Location) &&
                ownerLocation.WithinDistance(npc.Location, CreatureConstants.VisibilityDistance) &&
                npc.Appearance.Visible)
            {
                _visibleCreatures.Add(npc);
                _visibleNpcs.Add(npc);
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
            if (_owner is not ICharacter character) return;
            foreach (var region in _visibleRegions)
            {
                await _regionService.LoadRegionAsync(region);
                region.SendFullPartUpdates(character);
            }
        }

        public bool NeedsDynamicDraw() => _visibleRegions.Any(r => r.IsDynamic);

        public bool ShouldRebuild()
        {
            if (ViewLocation.Dimension != _owner.Location.Dimension) return true;
            var diffX = Math.Abs(ViewLocation.RegionPartX - _owner.Location.RegionPartX);
            var diffY = Math.Abs(ViewLocation.RegionPartY - _owner.Location.RegionPartY);
            var size = ((MapSize.Size >> 3) / 2) - 1;
            return diffX >= size || diffY >= size;
        }

        public bool InBounds(ILocation location) =>
            location.X >= BoundsMinimum.X && location.X <= BoundsMaximum.X &&
            location.Y >= BoundsMinimum.Y && location.Y <= BoundsMaximum.Y &&
            location.Z >= BoundsMinimum.Z && location.Z <= BoundsMaximum.Z;

        public bool InPreviousMapBounds(IEntity entity) => InPreviousMapBounds(entity.Location);

        public bool InPreviousMapBounds(ILocation location)
        {
            if (PreviousBoundsMinimum == null || PreviousBoundsMaximum == null) return false;
            return location.X >= PreviousBoundsMinimum.X && location.X <= PreviousBoundsMaximum.X &&
                   location.Y >= PreviousBoundsMinimum.Y && location.Y <= PreviousBoundsMaximum.Y &&
                   location.Z >= PreviousBoundsMinimum.Z && location.Z <= PreviousBoundsMaximum.Z;
        }

        public void GetPreviousLocalPosition(ILocation location, ref int mapX, ref int mapY)
        {
            mapX = location.X - PreviousBaseX;
            mapY = location.Y - PreviousBaseY;
        }

        public void GetLocalPosition(ILocation location, ref int mapX, ref int mapY)
        {
            mapX = location.X - BaseAbsX;
            mapY = location.Y - BaseAbsY;
        }

        /// <summary>
        /// Internal state used for viewport updates to avoid closure allocations.
        /// </summary>
        internal readonly struct ViewportUpdateState
        {
            public readonly Viewport Viewport;
            public readonly ILocation OwnerLocation;

            public ViewportUpdateState(Viewport viewport, ILocation ownerLocation)
            {
                Viewport = viewport;
                OwnerLocation = ownerLocation;
            }
        }
    }
}
