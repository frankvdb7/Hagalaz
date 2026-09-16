using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterServiceRegistrationTests
{
    [TestMethod]
    public async Task AddAndRemoveAsync_UpdatesCharacterAndEntityStoresTogether()
    {
        var entityStore = new EntityStore();
        var character = EntityTestFactory.Create<ICharacter>();
        character.MasterId.Returns(42u);
        var service = new CharacterService(CreateCharacterStore(), entityStore);

        Assert.IsTrue(await service.AddAsync(character));
        var handle = character.Handle;
        Assert.IsTrue(entityStore.TryResolve(handle, out var resolved));
        Assert.AreSame(character, resolved);

        Assert.IsTrue(await service.RemoveAsync(character));
        Assert.AreEqual(handle, character.Handle);
        Assert.IsFalse(entityStore.TryResolve(handle, out _));
    }

    [TestMethod]
    public async Task Remove_UpdatesCharacterAndEntityStoresTogether()
    {
        var entityStore = new EntityStore();
        var character = EntityTestFactory.Create<ICharacter>();
        character.MasterId.Returns(42u);
        var service = new CharacterService(CreateCharacterStore(), entityStore);

        Assert.IsTrue(await service.AddAsync(character));
        var handle = character.Handle;

        Assert.IsTrue(service.Remove(character));
        Assert.IsFalse(entityStore.TryResolve(handle, out _));
    }

    private static CharacterStore CreateCharacterStore() => new(Options.Create(new GameServerOptions
    {
        ClientRevision = 1,
        ClientRevisionPatch = 0,
        AuthenticationToken = "test"
    }));
}
