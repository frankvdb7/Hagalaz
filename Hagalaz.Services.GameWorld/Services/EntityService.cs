using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;

namespace Hagalaz.Services.GameWorld.Services;

public sealed class EntityService(IEntityStore entityStore) : IEntityService
{
    public bool TryResolve<TEntity>(EntityHandle<TEntity> handle, out TEntity? entity)
        where TEntity : class, IEntity => entityStore.TryResolve(handle, out entity);
}
