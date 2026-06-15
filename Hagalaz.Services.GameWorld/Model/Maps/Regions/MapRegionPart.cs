using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.Updates;
using Hagalaz.Game.Messages.Protocol.Map;
using Hagalaz.Game.Utilities;
using Hagalaz.Services.GameWorld.Model.Maps.Regions.Updates;
using Raido.Common.Protocol;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Model.Maps.Regions
{
    /// <summary>
    /// Contains region part data.
    /// Each region has 4x8x8 parts
    /// </summary>
    public class MapRegionPart : IMapRegionPart
    {
        private readonly IMapper _mapper;
        private readonly IGroundItemBuilder _groundItemBuilder;
        private readonly Dictionary<int, List<IGroundItem>> _groundItems = new();
        private readonly Dictionary<int, IGameObject> _gameObjects = new();
        private readonly Dictionary<int, IGameObject> _disabledStaticGameObjects = new();
        private readonly List<IRegionPartUpdate> _updates = [];

        public MapRegionPart(IMapper mapper, IGroundItemBuilder groundItemBuilder)
        {
            _mapper = mapper;
            _groundItemBuilder = groundItemBuilder;
        }

        /// <summary>
        /// Contains real (drawed) part X
        /// If this part data is not modified this is same
        /// as the PartX of this data.
        /// </summary>
        public int DrawRegionPartX { get; set; }

        /// <summary>
        /// Contains real (drawed) part Y
        /// If this part data is not modified this is same
        /// as the PartY of this data.
        /// </summary>
        public int DrawRegionPartY { get; set; }

        /// <summary>
        /// Contains real (drawed) Z
        /// If this part data is not modified this is same
        /// as the Z of this data.
        /// </summary>
        public int DrawRegionZ { get; set; }

        /// <summary>
        /// Contains rotation of this sector,
        /// It is 0 by default if not modified.
        /// </summary>
        public int Rotation { get; set; }

        public IEnumerable<IGameObject> FindAllGameObjects() => _gameObjects.Values;

        public IGameObject? FindGameObject(LayerType layer, int localX, int localY, int z)
        {
            var localHash = GameObjectHelper.GetRegionLocalHash(localX, localY, z, (int)layer);
            return _gameObjects.TryGetValue(localHash, out var gameObject) ? gameObject : null;
        }

        public IEnumerable<IGroundItem> FindAllGroundItems() => _groundItems.Values.SelectMany(itemsOnLocation => itemsOnLocation);

        public void Add(IGameObject gameObject)
        {
            var localHash = gameObject.GetRegionLocalHash();
            _gameObjects.TryGetValue(localHash, out var gameObjectOnLocation);
            if (gameObjectOnLocation == gameObject)
            {
                throw new InvalidOperationException($"GameObject {gameObject} is already added to this region!");
            }

            if (gameObjectOnLocation != null)
            {
                Remove(gameObjectOnLocation);
                _gameObjects[localHash] = gameObject;
            }
            else
            {
                _gameObjects.Add(localHash, gameObject);
            }

            IGameObject? disabledStaticGameObject = null;
            if (gameObject.IsStatic)
            {
                _disabledStaticGameObjects.Remove(localHash, out disabledStaticGameObject);
                gameObject.Enable();
            }

            gameObject.OnSpawn();

            if (!gameObject.IsStatic || disabledStaticGameObject != null)
            {
                QueueUpdate(new AddGameObjectUpdate(gameObject));
            }
        }

        public void Add(IGroundItem item)
        {
            if (_groundItems.Values.SelectMany(list => list).Contains(item))
            {
                throw new InvalidOperationException($"GroundItem {item} is already added to this region!");
            }

            var localHash = item.Location.GetRegionLocalHash();
            if (!_groundItems.TryGetValue(localHash, out var itemsOnLocation))
            {
                itemsOnLocation = [];
                _groundItems.Add(localHash, itemsOnLocation);
            }

            if (!item.CanRespawn())
            {
                // merge ground items if applicable
                foreach (var it in itemsOnLocation)
                {
                    if (it.Owner != item.Owner || !it.CanStack(item) || it.ItemOnGround.Count <= 0 || item.ItemOnGround.Count <= 0)
                    {
                        continue;
                    }

                    long total = it.ItemOnGround.Count + (long)item.ItemOnGround.Count;
                    if (total <= 0 || total > int.MaxValue)
                    {
                        continue;
                    }

                    var oldCount = it.ItemOnGround.Count;
                    it.ItemOnGround.Count = (int)total;
                    QueueUpdate(new SetGroundItemCountUpdate(it, oldCount));
                    return;
                }
            }

            item.OnSpawn();
            itemsOnLocation.Add(item);
            QueueUpdate(new AddGroundItemUpdate(item));
        }

        public void Remove(IGameObject gameObject)
        {
            var localHash = gameObject.GetRegionLocalHash();
            if (!_gameObjects.TryGetValue(localHash, out var gameObjectOnLocation))
            {
                return;
            }

            if (gameObjectOnLocation != gameObject)
            {
                return;
            }

            _gameObjects.Remove(localHash);
            if (gameObject.IsStatic)
            {
                _disabledStaticGameObjects.Add(localHash, gameObject);
                gameObject.Disable();
            }
            else
            {
                gameObject.Destroy();
            }

            QueueUpdate(new RemoveGameObjectUpdate(gameObject));
        }

        public void Remove(IGroundItem item)
        {
            var localHash = item.Location.GetRegionLocalHash();
            if (!_groundItems.TryGetValue(localHash, out var itemsOnLocation))
            {
                return;
            }

            // only refresh the item when it is 'visible' and not waiting to respawn
            if (!item.IsRespawning)
            {
                QueueUpdate(new RemoveGroundItemUpdate(item));
            }

            itemsOnLocation.Remove(item);

            if (item.CanRespawn() && !item.IsRespawning)
            {
                item.Destroy();

                var respawnBuilder = _groundItemBuilder
                    .Create()
                    .WithItem(item.ItemOnGround.Clone())
                    .WithLocation(item.Location.Clone())
                    .WithRespawnTicks(item.RespawnTicks)
                    .WithTicks(item.RespawnTicks)
                    .AsRespawning();

                if (item.Owner != null)
                {
                    respawnBuilder = respawnBuilder.WithOwner(item.Owner);
                }

                var respawnItem = respawnBuilder.Build();
                itemsOnLocation.Add(respawnItem);
            }
            else
            {
                item.Destroy();
            }

            if (itemsOnLocation.Count <= 0)
            {
                _groundItems.Remove(localHash);
            }
        }

        /// <summary>
        /// Handles a ground item whose timer has run out by either respawning
        /// it, converting it to a public item, or removing it entirely.
        /// </summary>
        /// <param name="item">The ground item whose timer expired.</param>
        public void ProcessExpiredItem(IGroundItem item)
        {
            var localHash = item.Location.GetRegionLocalHash();
            if (!_groundItems.TryGetValue(localHash, out var itemsOnLocation))
            {
                return;
            }

            // Always remove the expired item first
            itemsOnLocation.Remove(item);

            if (!item.IsRespawning && item.CanRespawn())
            {
                // Replace with a respawning item
                var respawnItem = _groundItemBuilder
                    .Create()
                    .WithItem(item.ItemOnGround.Clone())
                    .WithLocation(item.Location.Clone())
                    .WithRespawnTicks(item.RespawnTicks)
                    .WithTicks(item.RespawnTicks)
                    .AsRespawning()
                    .Build();
                itemsOnLocation.Add(respawnItem);
                return;
            }

            if (item.IsRespawning)
            {
                // Replace with a normal (non-respawning) item
                var normalItem = _groundItemBuilder
                    .Create()
                    .WithItem(item.ItemOnGround.Clone())
                    .WithLocation(item.Location.Clone())
                    .WithRespawnTicks(item.RespawnTicks)
                    .WithTicks(item.RespawnTicks)
                    // Do NOT call AsRespawning here!
                    .Build();
                itemsOnLocation.Add(normalItem);
                return;
            }

            item.Destroy();

            if (!item.IsPublic && item.ItemOnGround.ItemScript.CanTradeItem(item.ItemOnGround, item.Owner))
            {
                var publicGroundItem = _groundItemBuilder
                    .Create()
                    .WithItem(item.ItemOnGround.Clone())
                    .WithLocation(item.Location.Clone())
                    .WithRespawnTicks(0)
                    .Build();
                itemsOnLocation.Add(publicGroundItem);
            }

            if (itemsOnLocation.Count <= 0)
            {
                _groundItems.Remove(localHash);
            }
        }

        public void SendFullUpdate(ICharacter character)
        {
            var updates = new List<IRegionPartUpdate>();
            updates.AddRange(FindAllGroundItems().Select(item => new AddGroundItemUpdate(item)));
            updates.AddRange(FindAllGameObjects().Where(obj => !obj.IsStatic).Select(obj => new AddGameObjectUpdate(obj)));
            updates.AddRange(_disabledStaticGameObjects.Values.Select(obj => new RemoveGameObjectUpdate(obj)));
            SendUpdates(character, updates, true);
        }

        public void SendUpdates(ICharacter character)
        {
            if (_updates.Count <= 0)
            {
                return;
            }

            SendUpdates(character, (IReadOnlyList<IRegionPartUpdate>)_updates, false);
        }

        public void SendUpdates(ICharacter character, IReadOnlyList<IRegionPartUpdate> updates, bool fullUpdate)
        {
            // Optimization: Avoid LINQ and ToList() heap allocations for IReadOnlyList (hot path).
            // Reduces filtering overhead by using pooled buffers and indexed loops.
            int count = updates.Count;
            if (count == 0) return;

            var buffer = ArrayPool<IRegionPartUpdate>.Shared.Rent(count);
            try
            {
                int updateableCount = 0;
                for (int i = 0; i < count; i++)
                {
                    var update = updates[i];
                    if (update.CanUpdateFor(character)) buffer[updateableCount++] = update;
                }

                if (updateableCount > 0) SendUpdateMessages(character, buffer, updateableCount, fullUpdate);
            }
            finally
            {
                // clearArray: true is required for reference types to prevent memory leaks.
                ArrayPool<IRegionPartUpdate>.Shared.Return(buffer, clearArray: true);
            }
        }

        public void SendUpdates(ICharacter character, IEnumerable<IRegionPartUpdate> updates, bool fullUpdate)
        {
            // Fallback for pure IEnumerable (e.g. lazy LINQ queries).
            // Uses ToList() to avoid double-enumeration and potential IndexOutOfRangeException.
            var updateables = updates.Where(u => u.CanUpdateFor(character)).ToList();
            if (updateables.Count > 0)
            {
                SendUpdateMessages(character, updateables, updateables.Count, fullUpdate);
            }
        }

        private void SendUpdateMessages(ICharacter character, IReadOnlyList<IRegionPartUpdate> buffer, int count, bool fullUpdate)
        {
            var baseLocation = new Location(DrawRegionPartX << 3, DrawRegionPartY << 3, DrawRegionZ, 0);
            int localX = -1, localY = -1;
            character.Viewport.GetLocalPosition(baseLocation, ref localX, ref localY);

            character.Session.SendMessage(new MapRegionPartUpdateMessage
            {
                LocalX = localX, LocalY = localY, Z = DrawRegionZ, FullUpdate = fullUpdate
            });

            for (int i = 0; i < count; i++)
            {
                var update = buffer[i];
                var message = _mapper.Map<RaidoMessage>(update);
                character.Session.SendMessage(message);
                update.OnUpdatedFor(character);
            }
        }

        public void QueueUpdate(IRegionPartUpdate update)
        {
            if (_updates.Any(u => u.Equals(update)))
            {
                return;
            }

            _updates.Add(update);
        }

        public void ClearUpdates() => _updates.Clear();

        public void Erase()
        {
            DrawRegionPartX = 0;
            DrawRegionPartY = 0;
            DrawRegionZ = 0;
            Rotation = 0;
        }

        public override int GetHashCode() =>
            ((Rotation & 0x3) << 1) | ((DrawRegionZ & 0x3) << 24) | ((DrawRegionPartX & 0x3ff) << 14) | ((DrawRegionPartY & 0x7ff) << 3);
    }
}
