using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Services.GameWorld.Store;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

internal static class EntityTestFactory
{
    public static T Create<T>() where T : class, IEntity
    {
        var entity = Substitute.For<T, IEntityIdentity>();
        var identity = (IEntityIdentity)entity;
        entity.Handle.Returns(_ => identity.Handle);
        return entity;
    }
}
