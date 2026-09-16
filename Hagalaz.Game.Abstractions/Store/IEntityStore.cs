using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Store;

/// <summary>
/// Owns the process-wide identity mapping for live entities.
/// </summary>
public interface IEntityStore
{
    EntityHandle Add(IEntity entity);

    bool Remove(IEntity entity);

    bool TryResolve(EntityHandle handle, out IEntity? entity);
}
