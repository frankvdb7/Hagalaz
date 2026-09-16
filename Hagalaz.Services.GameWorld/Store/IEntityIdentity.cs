using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Services.GameWorld.Store;

/// <summary>
/// Allows the entity store to assign an identity without exposing mutation to gameplay code.
/// </summary>
internal interface IEntityIdentity : IEntity
{
    new EntityHandle Handle { get; set; }
}
