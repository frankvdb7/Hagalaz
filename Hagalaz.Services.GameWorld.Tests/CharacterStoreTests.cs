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
}
