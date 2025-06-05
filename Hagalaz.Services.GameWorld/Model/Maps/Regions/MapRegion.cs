using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Collections;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Configuration;
using Microsoft.Extensions.Options;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions
{
    /// <summary>
    /// Represents a single region.
    /// </summary>
    public partial class MapRegion : IMapRegion
    {
        private readonly ConcurrentStore<int, ICharacter> _characters = new();
        private readonly ConcurrentStore<int, INpc> _npcs = new();
        private readonly ConcurrentStore<int, IMapRegionPart> _parts = new();
        private readonly CollisionFlag[,,] _collision;
        private DateTime _idleTime = DateTime.MinValue;
        private readonly INpcService _npcService;
        private readonly IMapRegionService _regionService;
        private readonly IGameObjectBuilder _gameObjectBuilder;
        private readonly IGroundItemBuilder _groundItemBuilder;
        private readonly IMapper _mapper;
        private readonly IOptions<GroundItemOptions> _groundItemOptions;

        public int Id => BaseLocation.RegionId;
        public ILocation BaseLocation { get; }
        public IVector3 Size { get; }
        public bool IsDynamic { get; private set; }
        public bool IsLoaded { get; private set; }
        public bool IsDestroyed { get; private set; }
        public int[] XteaKeys { get; }

        public MapRegion(
            ILocation baseLocation,
            int[] xtea,
            INpcService npcService,
            IMapRegionService regionService,
            IGameObjectBuilder gameObjectBuilder,
            IGroundItemBuilder groundItemBuilder,
            IOptions<GroundItemOptions> groundItemOptions,
            IMapper mapper)
        {
            BaseLocation = baseLocation;
            Size = Location.Create(64, 64, 4);
            _collision = new CollisionFlag[Size.Z, Size.X, Size.Y];
            XteaKeys = xtea;

            _npcService = npcService;
            _regionService = regionService;
            _gameObjectBuilder = gameObjectBuilder;
            _groundItemBuilder = groundItemBuilder;
            _groundItemOptions = groundItemOptions;
            _mapper = mapper;
        }

        public void Add(INpc npc)
        {
            if (!_npcs.TryAdd(npc.Index, npc))
            {
                throw new InvalidOperationException($"Npc {npc} is already added to this region");
            }
        }

        public void Add(ICharacter character)
        {
            if (!_characters.TryAdd(character.Index, character))
            {
                throw new InvalidOperationException($"Character {character} is already added to this region");
            }
        }

        public void Remove(ICharacter character) => _characters.TryRemove(character.Index);

        public void Remove(INpc npc) => _npcs.TryRemove(npc.Index);

        public IEnumerable<ICharacter> FindAllCharacters() => _characters;

        public IEnumerable<INpc> FindAllNpcs() => _npcs;

        public void SendFullPartUpdates(ICharacter character)
        {
            foreach (var part in _parts)
            {
                part.SendFullUpdate(character);
            }
        }

        /// <summary>
        /// Tick 1.
        /// </summary>
        public async Task MajorUpdateTick()
        {
            await Task.CompletedTask;
            foreach (var character in _characters)
            {
                character.MajorUpdateTick();
            }

            foreach (var npc in _npcs)
            {
                npc.MajorUpdateTick();
            }
        }

        /// <summary>
        /// Tick 2.
        /// </summary>
        public async Task MajorClientPrepareUpdateTick()
        {
            TickGroundItems();

            foreach (var character in _characters)
            {
                await character.MajorClientPrepareUpdateTickAsync();
            }

            foreach (var npc in _npcs)
            {
                await npc.MajorClientPrepareUpdateTickAsync();
            }
        }

        /// <summary>
        /// Tick 3.
        /// </summary>
        public async Task MajorClientUpdateTick()
        {
            foreach (var part in _parts)
            {
                foreach (var character in _characters)
                {
                    part.SendUpdates(character);
                }
            }

            foreach (var character in _characters)
            {
                await character.MajorClientUpdateTickAsync();
            }

            foreach (var npc in _npcs)
            {
                await npc.MajorClientUpdateTickAsync();
            }
        }

        /// <summary>
        /// Tick 4.
        /// </summary>
        public async Task MajorClientUpdateResetTick()
        {
            // clear update things like projectiles & etc
            foreach (var part in _parts)
            {
                part.ClearUpdates();
            }

            foreach (var character in _characters)
            {
                await character.MajorClientUpdateResetTickAsync();
            }

            foreach (var npc in _npcs)
            {
                await npc.MajorClientUpdateResetTickAsync();
            }
        }

        public bool CanSuspend()
        {
            if (FindAllCharacters().Any(character => !character.CanSuspend()))
            {
                return false;
            }

            if (FindAllNpcs().Any(npc => !npc.CanSuspend()))
            {
                return false;
            }

            if (FindAllGroundItems().Any(item => !item.CanSuspend()))
            {
                return false;
            }

            if (Enumerable.Any<IGameObject>(FindAllGameObjects(), obj => !obj.CanSuspend()))
            {
                return false;
            }

            return true;
        }

        public bool CanDestroy()
        {
            if (IsDynamic)
            {
                return false;
            }

            if ((DateTime.Now - _idleTime).TotalMinutes <= 5)
            {
                return false;
            }

            if (FindAllCharacters().Any(character => !character.CanDestroy()))
            {
                return false;
            }

            if (FindAllNpcs().Any(npc => !npc.CanDestroy()))
            {
                return false;
            }

            if (FindAllGroundItems().Any(item => !item.CanDestroy()))
            {
                return false;
            }

            if (Enumerable.Any<IGameObject>(FindAllGameObjects(), obj => !obj.CanDestroy()))
            {
                return false;
            }

            return true;
        }

        public void Load() => IsLoaded = true;

        public void Resume() => _idleTime = DateTime.MinValue;

        public void Suspend() => _idleTime = DateTime.Now;

        public async Task DestroyAsync()
        {
            if (IsDestroyed)
            {
                throw new InvalidOperationException($"Region {this} is already destroyed");
            }

            foreach (var npc in FindAllNpcs())
            {
                await _npcService.UnregisterAsync(npc);
            }

            foreach (var item in FindAllGroundItems())
            {
                item.Destroy();
            }

            foreach (var obj in FindAllGameObjects())
            {
                obj.Destroy();
            }

            IsDestroyed = true;
        }

        public void QueueUpdate(IRegionPartUpdate update)
        {
            var partHash = update.Location.GetRegionPartHash();
            _parts.GetOrAdd(partHash, CreateRegionPart).QueueUpdate(update);
        }

        public IMapRegionPart CreateRegionPart(int partHash) =>
            new MapRegionPart(_mapper)
            {
                DrawRegionPartX = partHash & 0x3ff, DrawRegionPartY = (partHash >> 10) & 0x7ff, DrawRegionZ = (partHash >> 21) & 0x3,
            };
    }
}