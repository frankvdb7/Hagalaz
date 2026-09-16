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
    private readonly Dictionary<IEntity, EntityHandle> _handles = new(ReferenceEqualityComparer.Instance);

    public EntityHandle Add(IEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        lock (_gate)
        {
            if (_handles.TryGetValue(entity, out var existing))
            {
                return existing;
            }

            var slot = _freeSlots.Count > 0 ? _freeSlots.Pop() : _entities.Count;
            if (slot == _entities.Count)
            {
                _entities.Add(entity);
                _generations.Add(1);
            }
            else
            {
                _entities[slot] = entity;
            }

            var handle = new EntityHandle(slot, _generations[slot]);
            _handles.Add(entity, handle);
            return handle;
        }
    }

    public bool Remove(IEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        lock (_gate)
        {
            if (!_handles.Remove(entity, out var handle)
                || handle.Slot <= 0
                || handle.Slot >= _entities.Count
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

    public bool TryGetHandle(IEntity entity, out EntityHandle handle)
    {
        ArgumentNullException.ThrowIfNull(entity);

        lock (_gate)
        {
            return _handles.TryGetValue(entity, out handle);
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
