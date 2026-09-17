using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Services;

/// <summary>
/// Resolves live entity identities for delayed gameplay work.
/// </summary>
public interface IEntityService
{
    bool TryResolve<TEntity>(EntityHandle<TEntity> handle, out TEntity? entity)
        where TEntity : class, IEntity;
}
