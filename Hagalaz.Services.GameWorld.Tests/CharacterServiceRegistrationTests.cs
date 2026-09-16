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
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var service = new CharacterService(CreateCharacterStore(), entityStore);

        Assert.IsTrue(await service.AddAsync(character));
        Assert.IsTrue(entityStore.TryGetHandle(character, out var handle));
        Assert.IsTrue(entityStore.TryResolve(handle, out var resolved));
        Assert.AreSame(character, resolved);

        Assert.IsTrue(await service.RemoveAsync(character));
        Assert.IsFalse(entityStore.TryGetHandle(character, out _));
        Assert.IsFalse(entityStore.TryResolve(handle, out _));
    }

    [TestMethod]
    public async Task Remove_UpdatesCharacterAndEntityStoresTogether()
    {
        var entityStore = new EntityStore();
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        var service = new CharacterService(CreateCharacterStore(), entityStore);

        Assert.IsTrue(await service.AddAsync(character));
        Assert.IsTrue(entityStore.TryGetHandle(character, out var handle));

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
