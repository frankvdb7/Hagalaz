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

    public EntityHandle Add(IEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (entity is not IEntityIdentity identity)
        {
            throw new InvalidOperationException($"Entity type '{entity.GetType()}' does not support store identity assignment.");
        }

        lock (_gate)
        {
            if (entity.Handle != default
                && TryResolve(entity.Handle, out var current)
                && ReferenceEquals(current, entity))
            {
                return entity.Handle;
            }

            var slot = _freeSlots.Count > 0 ? _freeSlots.Pop() : _entities.Count;
            if (slot == _entities.Count)
            {
                _entities.Add(null);
                _generations.Add(1);
            }

            var handle = new EntityHandle(slot, _generations[slot]);
            identity.Handle = handle;
            _entities[slot] = entity;
            return handle;
        }
    }

    public bool Remove(IEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        lock (_gate)
        {
            var handle = entity.Handle;
            if (handle.Slot <= 0
                || handle.Slot >= _entities.Count
                || handle.Generation == 0
                || _generations[handle.Slot] != handle.Generation
                || !ReferenceEquals(_entities[handle.Slot], entity))
            {
                return false;
            }

            _entities[handle.Slot] = null;
            _generations[handle.Slot] = NextGeneration(_generations[handle.Slot]);
            _freeSlots.Push(handle.Slot);
            return true;
        }
    }

    public bool TryResolve(EntityHandle handle, out IEntity? entity)
    {
        lock (_gate)
        {
            if (handle.Slot <= 0
                || handle.Slot >= _entities.Count
                || handle.Generation == 0
                || _generations[handle.Slot] != handle.Generation)
            {
                entity = null;
                return false;
            }

            entity = _entities[handle.Slot];
            return entity is not null;
        }
    }

    private static uint NextGeneration(uint generation) =>
        generation == uint.MaxValue ? 1u : generation + 1;
}
