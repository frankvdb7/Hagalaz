using System;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class NpcServiceRegistrationTests
{
    [TestMethod]
    public async Task RegisterAsync_WhenInitializationFails_RemovesTheGlobalEntryAndDestroysTheNpc()
    {
        var store = new NpcStore();
        var npc = CreateNpc();
        var failure = new InvalidOperationException("npc initialization failed");
        npc.OnRegistered().Returns(Task.FromException(failure));
        var service = CreateService(store);

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.RegisterAsync(npc));

        Assert.AreSame(failure, actual);
        Assert.AreEqual(0, await store.CountAsync());
        npc.Received(1).Destroy();
    }

    [TestMethod]
    public async Task RegisterAsync_WhenInitializationFailsAfterRegionAttachment_RollsBackBothOwners()
    {
        var store = new NpcStore();
        var region = Substitute.For<Hagalaz.Game.Abstractions.Model.Maps.IMapRegion>();
        var npc = CreateNpc();
        var failure = new InvalidOperationException("script initialization failed");
        npc.OnRegistered().Returns(_ =>
        {
            region.Add(npc);
            return Task.FromException(failure);
        });
        npc.When(value => value.Destroy()).Do(_ => region.Remove(npc));
        var service = CreateService(store);

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.RegisterAsync(npc));

        Assert.AreSame(failure, actual);
        Assert.AreEqual(0, await store.CountAsync());
        region.Received(1).Add(npc);
        region.Received(1).Remove(npc);
        npc.Received(1).Destroy();
    }

    [TestMethod]
    public async Task RegisterAsync_WhenGlobalStoreRejectsNpc_ReportsFailureAndDoesNotInitializeIt()
    {
        var store = Substitute.For<INpcStore>();
#pragma warning disable CA2012 // NSubstitute consumes the configured ValueTask.
        store.AddAsync(Arg.Any<INpc>()).Returns(_ => ValueTask.FromResult(false));
#pragma warning restore CA2012
        var npc = CreateNpc();
        var service = CreateService(store);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.RegisterAsync(npc));

        await npc.DidNotReceive().OnRegistered();
        npc.Received(1).Destroy();
    }

    [TestMethod]
    public async Task RegisterAsync_WhenSuccessfulPublishesExactlyOneEntryAndUnregisterRemovesIt()
    {
        var store = new NpcStore();
        var npc = CreateNpc();
        npc.OnRegistered().Returns(Task.CompletedTask);
        var service = CreateService(store);

        await service.RegisterAsync(npc);
        Assert.AreEqual(1, await store.CountAsync());
        await npc.Received(1).OnRegistered();

        await service.UnregisterAsync(npc);

        Assert.AreEqual(0, await store.CountAsync());
        npc.Received(1).Destroy();
    }

    [TestMethod]
    public async Task RegisterAsync_WhenFirstAttemptFails_AllowsASecondAttemptWithoutDuplicateOwnership()
    {
        var store = new NpcStore();
        var firstNpc = CreateNpc();
        var attempts = 0;
        var failure = new InvalidOperationException("first attempt failed");
        firstNpc.OnRegistered().Returns(_ =>
            ++attempts == 1 ? Task.FromException(failure) : Task.CompletedTask);
        var service = CreateService(store);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.RegisterAsync(firstNpc));
        Assert.AreEqual(0, await store.CountAsync());

        var secondNpc = CreateNpc();
        await service.RegisterAsync(secondNpc);

        Assert.AreEqual(1, await store.CountAsync());
        await secondNpc.Received(1).OnRegistered();
        firstNpc.Received(1).Destroy();
    }

    [TestMethod]
    public async Task RegisterAsync_WhenNpcIsDestroyed_RejectsRegistration()
    {
        var store = new NpcStore();
        var npc = CreateNpc();
        npc.IsDestroyed.Returns(true);
        var service = CreateService(store);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.RegisterAsync(npc));

        await npc.DidNotReceive().OnRegistered();
        Assert.AreEqual(0, await store.CountAsync());
    }

    private static INpc CreateNpc()
    {
        var npc = Substitute.For<INpc>();
        npc.IsDestroyed.Returns(false);
        return npc;
    }

    private static NpcService CreateService(INpcStore store)
    {
        var definitionStore = new NpcDefinitionStore(
            Substitute.For<IServiceProvider>(),
            Substitute.For<ITypeProvider<INpcType>>(),
            Substitute.For<IMapper>(),
            NullLogger<NpcDefinitionStore>.Instance);
        return new NpcService(
            store,
            definitionStore,
            Substitute.For<ITypeProvider<INpcDefinition>>(),
            NullLogger<NpcService>.Instance);
    }
}
