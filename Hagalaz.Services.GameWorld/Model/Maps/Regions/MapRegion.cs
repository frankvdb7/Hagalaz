using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Extensions;
using Microsoft.AspNetCore.Connections;

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
        private readonly SemaphoreSlim _destructionLock = new(1, 1);
        private readonly object _mutationGate = new();
        public int Id => BaseLocation.RegionId;
        public ILocation BaseLocation { get; }
        public IVector3 Size { get; }
        public bool IsDynamic { get; private set; }
        private int _state = (int)MapRegionState.Initializing;
        public MapRegionState State => (MapRegionState)Volatile.Read(ref _state);
        private int _destructionState = (int)MapRegionDestructionState.Active;
        public MapRegionDestructionState DestructionState => (MapRegionDestructionState)Volatile.Read(ref _destructionState);
        public bool IsDestroyed => DestructionState == MapRegionDestructionState.Destroyed;
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
            Size = Location.Create(64, 64, 4);
            _collision = new CollisionFlag[Size.Z, Size.X, Size.Y];
            XteaKeys = xtea;

            _npcService = npcService;
            _regionService = regionService;
            _gameObjectBuilder = gameObjectBuilder;
            _groundItemBuilder = groundItemBuilder;
            _mapper = mapper;
        }

        public void Add(INpc npc)
        {
            lock (_mutationGate)
            {
                EnsureAcceptsMutation();
                if (!_npcs.TryAdd(npc.Index, npc))
                {
                    throw new InvalidOperationException($"Npc {npc} is already added to this region");
                }
            }
        }

        public void Add(ICharacter character)
        {
            lock (_mutationGate)
            {
                EnsureAcceptsMutation();
                if (!_characters.TryAdd(character.Index, character))
                {
                    throw new InvalidOperationException($"Character {character} is already added to this region");
                }
            }
        }

        public void Remove(ICharacter character)
        {
            lock (_mutationGate)
            {
                if (DestructionState != MapRegionDestructionState.Active)
                {
                    return;
                }

                _characters.TryRemove(character.Index);
            }
        }

        public void Remove(INpc npc)
        {
            lock (_mutationGate)
            {
                if (DestructionState != MapRegionDestructionState.Active)
                {
                    return;
                }

                _npcs.TryRemove(npc.Index, npc);
            }
        }

        public IEnumerable<ICharacter> FindAllCharacters() => _characters;

        public IEnumerable<INpc> FindAllNpcs() => _npcs;

        private void ForEachCreature(Action<ICreature> action)
        {
            foreach (var character in _characters)
            {
                action(character);
            }

            foreach (var npc in _npcs)
            {
                action(npc);
            }
        }

        private void ForEachCreature(Action<ICharacter> characterAction, Action<INpc> npcAction)
        {
            foreach (var character in _characters)
            {
                characterAction(character);
            }

            foreach (var npc in _npcs)
            {
                npcAction(npc);
            }
        }

        private bool AnyCreature(Func<ICreature, bool> predicate)
        {
            return _characters.Any(predicate) || _npcs.Any(predicate);
        }

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
        public void MajorUpdateTick()
        {
            ForEachCreature(c => c.MajorUpdateTick());
        }

        /// <summary>
        /// Tick 2.
        /// </summary>
        public void MajorClientPrepareUpdateTick()
        {
            TickGroundItems();
            ForEachCreature(c => c.MajorClientPrepareUpdateTick());
            foreach (var part in _parts)
            {
                part.PrepareUpdatesForTick();
            }
        }

        /// <summary>
        /// Tick 3.
        /// </summary>
        public void MajorClientUpdateTick(IReadOnlyDictionary<int, ICharacter> characters)
        {
            foreach (var character in _characters)
            {
                try
                {
                    foreach (var part in _parts)
                    {
                        part.SendUpdates(character);
                    }
                    character.MajorClientUpdateTick(characters);
                }
                catch (ConnectionAbortedException)
                {
                    // The connection lifecycle owns this failure. Continue updating other characters.
                }
            }

            foreach (var npc in _npcs)
            {
                npc.MajorClientUpdateTick();
            }
        }

        /// <summary>
        /// Tick 4.
        /// </summary>
        public void MajorClientUpdateResetTick()
        {
            // clear update things like projectiles & etc
            foreach (var part in _parts)
            {
                part.CompleteUpdateTick();
            }

            ForEachCreature(c => c.MajorClientUpdateResetTick());
        }

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

        public bool CanDestroy()
        {
            if (DestructionState != MapRegionDestructionState.Active)
            {
                return false;
            }

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

        public void MarkReady()
        {
            if (Interlocked.CompareExchange(ref _state, (int)MapRegionState.Ready, (int)MapRegionState.Initializing) != (int)MapRegionState.Initializing)
            {
                throw new InvalidOperationException($"Region {this} cannot transition to ready from state {State}.");
            }
        }

        public void MarkDiscarded()
        {
            var previousState = Interlocked.CompareExchange(
                ref _state,
                (int)MapRegionState.Discarded,
                (int)MapRegionState.Initializing);
            if (previousState == (int)MapRegionState.Discarded)
            {
                return;
            }

            if (previousState != (int)MapRegionState.Initializing)
            {
                throw new InvalidOperationException($"Region {this} cannot transition to discarded from state {State}.");
            }
        }

        public void Resume() => _idleTime = DateTime.MinValue;

        public void Suspend() => _idleTime = DateTime.Now;

        public async Task DestroyAsync()
        {
            await _destructionLock.WaitAsync().ConfigureAwait(false);
            Exception? failure = null;
            try
            {
                INpc[] npcs;
                IGroundItem[] items;
                IGameObject[] objects;
                lock (_mutationGate)
                {
                    if (DestructionState == MapRegionDestructionState.Destroyed)
                    {
                        throw new InvalidOperationException($"Region {this} is already destroyed");
                    }

                    Interlocked.Exchange(ref _destructionState, (int)MapRegionDestructionState.Destroying);
                    npcs = _npcs.ToArray();
                    items = FindAllGroundItems().ToArray();
                    objects = FindAllGameObjects().ToArray();
                }

                foreach (var npc in npcs)
                {
                    try
                    {
                        await _npcService.UnregisterAsync(npc).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        failure ??= ex;
                    }
                    finally
                    {
                        RemoveNpcAfterDestruction(npc);
                    }
                }

                foreach (var item in items)
                {
                    if (item.IsDestroyed)
                    {
                        RemoveGroundItemAfterDestruction(item);
                        continue;
                    }

                    try
                    {
                        item.Destroy();
                    }
                    catch (Exception ex) { failure ??= ex; }
                    finally { RemoveGroundItemAfterDestruction(item); }
                }

                foreach (var obj in objects)
                {
                    if (obj.IsDestroyed)
                    {
                        RemoveGameObjectAfterDestruction(obj);
                        continue;
                    }

                    try
                    {
                        obj.Destroy();
                    }
                    catch (Exception ex) { failure ??= ex; }
                    finally { RemoveGameObjectAfterDestruction(obj); }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _destructionState, (int)MapRegionDestructionState.Destroyed);
                _destructionLock.Release();
            }

            if (failure is not null)
            {
                throw failure;
            }
        }

        private void EnsureAcceptsMutation()
        {
            if (DestructionState != MapRegionDestructionState.Active)
            {
                throw new InvalidOperationException($"Region {this} no longer accepts mutations because it is {DestructionState}.");
            }
        }

        public void QueueUpdate(IRegionPartUpdate update)
        {
            lock (_mutationGate)
            {
                EnsureAcceptsMutation();
                var partHash = update.Location.GetRegionPartHash();
                _parts.GetOrAdd(partHash, CreateRegionPart).QueueUpdate(update);
            }
        }

        public IMapRegionPart CreateRegionPart(int partHash)
        {
            lock (_mutationGate)
            {
                EnsureAcceptsMutation();
                return CreateRegionPartCore(partHash);
            }
        }

        private IMapRegionPart CreateRegionPartCore(int partHash) =>
            new MapRegionPart(_mapper, _groundItemBuilder)
            {
                DrawRegionPartX = partHash & 0x3ff,
                DrawRegionPartY = (partHash >> 10) & 0x7ff,
                DrawRegionZ = (partHash >> 21) & 0x3,
                DrawRegionDimension = BaseLocation.Dimension,
                HasDrawSource = true,
            };

        private void RemoveNpcAfterDestruction(INpc npc)
        {
            lock (_mutationGate)
            {
                _npcs.TryRemove(npc.Index, npc);
            }
        }

        private void RemoveGroundItemAfterDestruction(IGroundItem item)
        {
            lock (_mutationGate)
            {
                var partHash = item.Location.GetRegionPartHash();
                if (_parts.TryGetValue(partHash, out var part) && part is MapRegionPart concretePart)
                {
                    concretePart.RemoveDestroyed(item);
                }
            }
        }

        private void RemoveGameObjectAfterDestruction(IGameObject gameObject)
        {
            lock (_mutationGate)
            {
                var partHash = gameObject.Location.GetRegionPartHash();
                if (_parts.TryGetValue(partHash, out var part) && part is MapRegionPart concretePart)
                {
                    concretePart.RemoveDestroyed(gameObject);
                    UnFlagCollision(gameObject);
                }
            }
        }
    }
}
