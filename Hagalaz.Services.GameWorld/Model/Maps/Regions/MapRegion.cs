using System.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Collections;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Extensions;

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

        /// <inheritdoc />
        public int Id => BaseLocation.RegionId;

        /// <inheritdoc />
        public ILocation BaseLocation { get; }

        /// <inheritdoc />
        public IVector3 Size { get; }

        /// <inheritdoc />
        public bool IsDynamic { get; private set; }

        /// <inheritdoc />
        public bool IsLoaded { get; private set; }

        /// <inheritdoc />
        public bool IsDestroyed { get; private set; }

        /// <inheritdoc />
        public int[] XteaKeys { get; }

        public MapRegion(
            ILocation baseLocation,
            int[] xtea,
            INpcService npcService,
            IMapRegionService regionService,
            IGameObjectBuilder gameObjectBuilder,
            IGroundItemBuilder groundItemBuilder,
            IMapper mapper)
        {
            BaseLocation = baseLocation;
            XteaKeys = xtea;
            _npcService = npcService;
            _regionService = regionService;
            _gameObjectBuilder = gameObjectBuilder;
            _groundItemBuilder = groundItemBuilder;
            _mapper = mapper;
            Size = new Vector3(64, 64, 4);
            _collision = new CollisionFlag[Size.X, Size.Y, Size.Z];
        }

        /// <inheritdoc />
        public void Add(INpc npc)
        {
            if (!_npcs.TryAdd(npc.Index, npc))
            {
                throw new InvalidOperationException($"Npc {npc} is already added to this region");
            }
        }

        /// <inheritdoc />
        public void Add(ICharacter character)
        {
            if (!_characters.TryAdd(character.Index, character))
            {
                throw new InvalidOperationException($"Character {character} is already added to this region");
            }
        }

        /// <inheritdoc />
        public void Remove(ICharacter character) => _characters.TryRemove(character.Index);

        /// <inheritdoc />
        public void Remove(INpc npc) => _npcs.TryRemove(npc.Index);

        /// <inheritdoc />
        public void ForEachCharacter<TState>(Action<ICharacter, TState> action, TState state)
        {
            foreach (var character in _characters)
            {
                action(character, state);
            }
        }

        /// <inheritdoc />
        public void ForEachNpc<TState>(Action<INpc, TState> action, TState state)
        {
            foreach (var npc in _npcs)
            {
                action(npc, state);
            }
        }

        /// <inheritdoc />
        public IEnumerable<ICharacter> FindAllCharacters() => _characters;

        /// <inheritdoc />
        public IEnumerable<INpc> FindAllNpcs() => _npcs;

        /// <summary>
        /// Executes an action for each creature in the region.
        /// </summary>
        private void ForEachCreature(Action<ICreature> action)
        {
            foreach (var character in _characters) action(character);
            foreach (var npc in _npcs) action(npc);
        }

        /// <summary>
        /// Asynchronously executes an action for each creature in the region.
        /// </summary>
        private async Task ForEachCreatureAsync(Func<ICreature, Task> action)
        {
            foreach (var character in _characters) await action(character);
            foreach (var npc in _npcs) await action(npc);
        }

        /// <summary>
        /// Determines whether any creature in the region satisfies a condition.
        /// </summary>
        private bool AnyCreature(Func<ICreature, bool> predicate) =>
            _characters.Any(predicate) || _npcs.Any(predicate);

        /// <inheritdoc />
        public void MakeStandard() => IsDynamic = false;

        /// <inheritdoc />
        public void MakeDynamic() => IsDynamic = true;

        /// <inheritdoc />
        public void Add(IGroundItem item)
        {
            var partHash = item.Location.GetRegionPartHash();
            _parts.GetOrAdd(partHash, CreateRegionPart).Add(item);
        }

        /// <inheritdoc />
        public void Add(IGameObject gameObj)
        {
            var partHash = gameObj.Location.GetRegionPartHash();
            _parts.GetOrAdd(partHash, CreateRegionPart).Add(gameObj);
        }

        /// <inheritdoc />
        public void Remove(IGameObject gameObj)
        {
            var partHash = gameObj.Location.GetRegionPartHash();
            if (_parts.TryGetValue(partHash, out var part))
            {
                part.Remove(gameObj);
            }
        }

        /// <inheritdoc />
        public void Remove(IGroundItem item)
        {
            var partHash = item.Location.GetRegionPartHash();
            if (_parts.TryGetValue(partHash, out var part))
            {
                part.Remove(item);
            }
        }

        /// <inheritdoc />
        public IEnumerable<IGroundItem> FindAllGroundItems()
        {
            foreach (var part in _parts)
            {
                foreach (var item in part.GroundItems)
                {
                    yield return item;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<IGameObject> FindAllGameObjects()
        {
            foreach (var part in _parts)
            {
                foreach (var obj in part.GameObjects)
                {
                    yield return obj;
                }
            }
        }

        /// <inheritdoc />
        public void FlagCollision(int localX, int localY, int z, CollisionFlag flag) => _collision[localX, localY, z] |= flag;

        /// <inheritdoc />
        public void UnFlagCollision(int localX, int localY, int z, CollisionFlag flag) => _collision[localX, localY, z] &= ~flag;

        /// <inheritdoc />
        public void UnFlagCollision(IGameObject gameObject)
        {
            // TODO
        }

        /// <inheritdoc />
        public void FlagCollision(IGameObject gameObject)
        {
            // TODO
        }

        /// <inheritdoc />
        public async Task MajorUpdateTick()
        {
            await Task.CompletedTask;
            ForEachCreature(c => c.MajorUpdateTick());
        }

        /// <inheritdoc />
        public async Task MajorClientPrepareUpdateTick()
        {
            TickGroundItems();
            await ForEachCreatureAsync(c => c.MajorClientPrepareUpdateTickAsync());
        }

        private void TickGroundItems()
        {
            foreach (var part in _parts)
            {
                part.TickGroundItems();
            }
        }

        /// <inheritdoc />
        public async Task MajorClientUpdateTick()
        {
            foreach (var character in _characters)
            {
                foreach (var part in _parts)
                {
                    part.SendUpdates(character);
                }
            }

            await ForEachCreatureAsync(c => c.MajorClientUpdateTickAsync());
        }

        /// <inheritdoc />
        public async Task MajorClientUpdateResetTick()
        {
            foreach (var part in _parts)
            {
                part.ClearUpdates();
            }

            await ForEachCreatureAsync(c => c.MajorClientUpdateResetTickAsync());
        }

        /// <inheritdoc />
        public bool CanSuspend()
        {
            if (AnyCreature(c => !c.CanSuspend()))
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

        /// <inheritdoc />
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

            if (AnyCreature(c => !c.CanDestroy()))
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

        /// <inheritdoc />
        public void Load() => IsLoaded = true;

        /// <inheritdoc />
        public void Resume() => _idleTime = DateTime.MinValue;

        /// <inheritdoc />
        public void Suspend() => _idleTime = DateTime.Now;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void QueueUpdate(IRegionPartUpdate update)
        {
            var partHash = update.Location.GetRegionPartHash();
            _parts.GetOrAdd(partHash, CreateRegionPart).QueueUpdate(update);
        }

        /// <inheritdoc />
        public IMapRegionPart CreateRegionPart(int partHash) =>
            new MapRegionPart(_mapper, _groundItemBuilder)
            {
                DrawRegionPartX = partHash & 0x3ff, DrawRegionPartY = (partHash >> 10) & 0x7ff, DrawRegionZ = (partHash >> 21) & 0x3,
            };

        /// <inheritdoc />
        public void SendFullPartUpdates(ICharacter character)
        {
            foreach (var part in _parts)
            {
                part.SendFullUpdates(character);
            }
        }

        /// <inheritdoc />
        public IMapRegionPart GetRegionPartData(int partX, int partY, int z)
        {
            var partHash = Location.GetRegionPartHash(partX, partY, z);
            return _parts.GetOrAdd(partHash, CreateRegionPart);
        }

        /// <inheritdoc />
        public void WriteBlock(int partX, int partY, int z, int drawPartX, int drawPartY, int drawPartZ)
        {
            // TODO
        }

        /// <inheritdoc />
        public IGameObject? FindStandardGameObject(int localX, int localY, int z)
        {
            foreach (var part in _parts)
            {
                foreach (var obj in part.GameObjects)
                {
                    if (obj.Location.X == localX && obj.Location.Y == localY && obj.Location.Z == z)
                    {
                        return obj;
                    }
                }
            }
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<IGameObject> FindGameObjects(int localX, int localY, int z)
        {
            foreach (var part in _parts)
            {
                foreach (var obj in part.GameObjects)
                {
                    if (obj.Location.X == localX && obj.Location.Y == localY && obj.Location.Z == z)
                    {
                        yield return obj;
                    }
                }
            }
        }

        /// <inheritdoc />
        public CollisionFlag GetCollision(int localX, int localY, int z) => _collision[localX, localY, z];
    }
}
