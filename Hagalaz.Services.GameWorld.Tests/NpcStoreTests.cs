using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Creatures;
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

    [TestMethod]
    public async Task CreatureHandle_DoesNotResolveAfterNpcSlotReuse()
    {
        var store = new NpcStore();
        var original = Substitute.For<INpc>();
        var replacement = Substitute.For<INpc>();

        Assert.IsTrue(await store.AddAsync(original));
        Assert.IsTrue(store.TryGetHandle(original, out var originalHandle));
        Assert.IsTrue(store.Remove(original));
        Assert.IsTrue(await store.AddAsync(replacement));

        Assert.AreEqual(original.Index, replacement.Index);
        Assert.AreNotEqual(originalHandle.Generation, GetHandle(store, replacement).Generation);
        Assert.IsNull(store.Resolve(originalHandle));
        Assert.AreSame(replacement, store.Resolve(GetHandle(store, replacement)));
    }

    private static CreatureHandle<INpc> GetHandle(NpcStore store, INpc npc)
    {
        Assert.IsTrue(store.TryGetHandle(npc, out var handle));
        return handle;
    }
}
