using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Store;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CharacterStoreTests
{
    [TestMethod]
    public async Task AddAsync_RejectsDuplicateMasterId()
    {
        var store = new CharacterStore(Options.Create(new GameServerOptions
        {
            ClientRevision = 1,
            ClientRevisionPatch = 0,
            AuthenticationToken = "test"
        }));
        var first = Substitute.For<ICharacter>();
        var second = Substitute.For<ICharacter>();
        first.MasterId.Returns(42u);
        second.MasterId.Returns(42u);

        Assert.IsTrue(await store.AddAsync(first));
        Assert.IsFalse(await store.AddAsync(second));
        Assert.AreEqual(1, await store.CountAsync());
    }

    [TestMethod]
    public async Task FindByIdAsyncAndFindByIndexAsync_ReturnExactCharacters()
    {
        var store = CreateStore();
        var first = Substitute.For<ICharacter>();
        var second = Substitute.For<ICharacter>();
        first.MasterId.Returns(42u);
        first.Index.Returns(3);
        second.MasterId.Returns(43u);
        second.Index.Returns(4);

        Assert.IsTrue(await store.AddAsync(first));
        Assert.IsTrue(await store.AddAsync(second));

        Assert.AreSame(first, await store.FindByIdAsync(42));
        Assert.AreSame(second, await store.FindByIndexAsync(second.Index));
        Assert.IsNull(await store.FindByIdAsync(99));
        Assert.IsNull(await store.FindByIndexAsync(-1));
    }

    private static CharacterStore CreateStore() => new(Options.Create(new GameServerOptions
    {
        ClientRevision = 1,
        ClientRevisionPatch = 0,
        AuthenticationToken = "test"
    }));
}
