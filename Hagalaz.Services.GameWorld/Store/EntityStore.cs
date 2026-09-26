using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Store;

namespace Hagalaz.Services.GameWorld.Store;

public sealed class EntityStore : IEntityStore
{
    private readonly object _gate = new();
    private readonly List<IEntity?> _entities = [null];
    private readonly List<uint> _generations = [0];
    private readonly Stack<int> _freeSlots = [];

    public EntityHandle<TEntity> Add<TEntity>(IEntity<TEntity> entity)
        where TEntity : class, IEntity
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is not IEntityIdentity identity)
        {
            throw new InvalidOperationException($"Entity type '{entity.GetType()}' does not support store identity assignment.");
        }

        lock (_gate)
        {
            if (identity.HandleSlot > 0
                && identity.HandleGeneration != 0
                && TryResolve(identity.HandleSlot, identity.HandleGeneration, out var current)
                && ReferenceEquals(current, entity))
            {
                return new EntityHandle<TEntity>(identity.HandleSlot, identity.HandleGeneration);
            }

            var slot = _freeSlots.Count > 0 ? _freeSlots.Pop() : _entities.Count;
            if (slot == _entities.Count)
            {
                _entities.Add(null);
                _generations.Add(1);
            }

            var generation = _generations[slot];
            identity.SetHandle(slot, generation);
            _entities[slot] = entity;
            return new EntityHandle<TEntity>(slot, generation);
        }
    }

    public bool Remove<TEntity>(IEntity<TEntity> entity)
        where TEntity : class, IEntity
    {
        ArgumentNullException.ThrowIfNull(entity);

        lock (_gate)
        {
            if (entity is not IEntityIdentity identity
                || identity.HandleSlot <= 0
                || identity.HandleSlot >= _entities.Count
                || identity.HandleGeneration == 0
                || _generations[identity.HandleSlot] != identity.HandleGeneration
                || !ReferenceEquals(_entities[identity.HandleSlot], entity))
            {
                return false;
            }

            _entities[identity.HandleSlot] = null;
            _generations[identity.HandleSlot] = NextGeneration(_generations[identity.HandleSlot]);
            _freeSlots.Push(identity.HandleSlot);
            return true;
        }
    }

    public bool TryResolve<TEntity>(EntityHandle<TEntity> handle, out TEntity? entity)
        where TEntity : class, IEntity
    {
        lock (_gate)
        {
            if (!TryResolve(handle.Slot, handle.Generation, out var resolved))
            {
                entity = null;
                return false;
            }

            entity = (TEntity)resolved!;
            return true;
        }
    }

    private bool TryResolve(int slot, uint generation, out IEntity? entity)
    {
        if (slot <= 0
            || slot >= _entities.Count
            || generation == 0
            || _generations[slot] != generation)
        {
            entity = null;
            return false;
        }

        entity = _entities[slot];
        return entity is not null;
    }

    private static uint NextGeneration(uint generation) =>
        generation == uint.MaxValue ? 1u : generation + 1;
}
