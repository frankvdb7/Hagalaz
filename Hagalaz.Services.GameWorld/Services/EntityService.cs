using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;

namespace Hagalaz.Services.GameWorld.Services;

public sealed class EntityService(IEntityStore entityStore) : IEntityService
{
    public bool TryResolve<T>(EntityHandle handle, out T? entity) where T : class, IEntity
    {
        if (entityStore.TryResolve(handle, out var resolved) && resolved is T typed)
        {
            entity = typed;
            return true;
        }

        entity = null;
        return false;
    }
}
