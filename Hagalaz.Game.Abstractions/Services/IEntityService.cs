using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Abstractions.Services;

/// <summary>
/// Resolves live entity identities for delayed gameplay work.
/// </summary>
public interface IEntityService
{
    bool TryResolve<T>(EntityHandle handle, out T? entity) where T : class, IEntity;
}
