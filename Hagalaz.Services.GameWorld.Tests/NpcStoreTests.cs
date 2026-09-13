using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Services.GameWorld.Store;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class NpcStoreTests
{
    [TestMethod]
    public async Task FindByIndexAsync_UsesTheNpcIndex()
    {
        var store = new NpcStore();
        var firstNpc = Substitute.For<INpc>();
        var secondNpc = Substitute.For<INpc>();

        await store.AddAsync(firstNpc);
        await store.AddAsync(secondNpc);

        var actual = await store.FindByIndexAsync(secondNpc.Index);

        Assert.AreSame(secondNpc, actual);
    }

    [TestMethod]
    public async Task FindByIndexAsync_ReturnsNullForAnInvalidIndex()
    {
        var store = new NpcStore();

        var actual = await store.FindByIndexAsync(-1);

        Assert.IsNull(actual);
    }
}
