using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Store;

/// <summary>
/// Owns the process-wide identity mapping for live entities.
/// </summary>
public interface IEntityStore
{
    EntityHandle<TEntity> Add<TEntity>(IEntity<TEntity> entity)
        where TEntity : class, IEntity;

    bool Remove<TEntity>(IEntity<TEntity> entity)
        where TEntity : class, IEntity;

    bool TryResolve<TEntity>(EntityHandle<TEntity> handle, out TEntity? entity)
        where TEntity : class, IEntity;
}
