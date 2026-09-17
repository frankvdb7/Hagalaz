using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Services.GameWorld.Store;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

internal static class EntityTestFactory
{
    public static T Create<T>() where T : class, IEntity
    {
        var entity = Substitute.For<T, IEntityIdentity>();
        var identity = (IEntityIdentity)entity;
        var slot = 0;
        var generation = 0u;
        var handle = default(EntityHandle<ICreature>);
        var creature = (IEntity<ICreature>)entity;
        creature.Handle.Returns(_ => handle);
        identity.HandleSlot.Returns(_ => slot);
        identity.HandleGeneration.Returns(_ => generation);
        identity.When(value => value.SetHandle(Arg.Any<int>(), Arg.Any<uint>()))
            .Do(callInfo =>
            {
                slot = callInfo.Arg<int>();
                generation = callInfo.Arg<uint>();
                handle = new EntityHandle<ICreature>(slot, generation);
            });
        return entity;
    }
}
