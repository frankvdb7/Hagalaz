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
        npc.When(value => value.OnRegistered()).Do(_ => throw failure);
        var service = CreateService(store);

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.RegisterAsync(npc));

        Assert.AreSame(failure, actual);
        Assert.AreEqual(0, await store.CountAsync());
        npc.Received(1).Destroy();
    }

    [TestMethod]
    public async Task Register_WhenInitializationCompletesSynchronously_PublishesTheNpcAndInitializesIt()
    {
        var store = new NpcStore();
        var npc = CreateNpc();
        var service = CreateService(store);

        service.Register(npc);

        Assert.AreEqual(1, await store.CountAsync());
        npc.Received(1).OnRegistered();
    }

    [TestMethod]
    public async Task Register_WhenInitializationFails_RemovesTheGlobalEntryAndDestroysTheNpc()
    {
        var store = new NpcStore();
        var npc = CreateNpc();
        var registrationFailure = new InvalidOperationException("npc initialization failed");
        npc.When(value => value.OnRegistered()).Do(_ => throw registrationFailure);
        var service = CreateService(store);

        var actual = Assert.ThrowsExactly<InvalidOperationException>(() => service.Register(npc));

        Assert.AreSame(registrationFailure, actual);
        Assert.AreEqual(0, await store.CountAsync());
        npc.Received(1).Destroy();
    }

    [TestMethod]
    public async Task Register_WhenInitializationAndCleanupFail_PreservesInitializationFailure()
    {
        var store = new NpcStore();
        var npc = CreateNpc();
        var registrationFailure = new InvalidOperationException("npc initialization failed");
        var cleanupFailure = new ApplicationException("npc cleanup failed");
        npc.When(value => value.OnRegistered()).Do(_ => throw registrationFailure);
        npc.When(value => value.Destroy()).Do(_ => throw cleanupFailure);
        var service = CreateService(store);

        var actual = Assert.ThrowsExactly<InvalidOperationException>(() => service.Register(npc));

        Assert.AreSame(registrationFailure, actual);
        Assert.AreEqual(0, await store.CountAsync());
        npc.Received(1).Destroy();
    }

    [TestMethod]
    public void Register_WhenCleanupOperationsFail_PreservesRegistrationFailure()
    {
        var store = Substitute.For<INpcStore>();
        var npc = CreateNpc();
        var registrationFailure = new InvalidOperationException("npc initialization failed");
        var removalFailure = new ApplicationException("npc removal failed");
        var destroyFailure = new NotSupportedException("npc destruction failed");
        store.Add(npc).Returns(true);
        store.When(value => value.Remove(npc)).Do(_ => throw removalFailure);
        npc.When(value => value.OnRegistered()).Do(_ => throw registrationFailure);
        npc.When(value => value.Destroy()).Do(_ => throw destroyFailure);
        var service = CreateService(store);

        var actual = Assert.ThrowsExactly<InvalidOperationException>(() => service.Register(npc));

        Assert.AreSame(registrationFailure, actual);
        store.Received(1).Remove(npc);
        npc.Received(1).Destroy();
    }

    [TestMethod]
    public async Task Unregister_WhenCalledSynchronously_RemovesTheNpc()
    {
        var store = new NpcStore();
        var npc = CreateNpc();
        store.Add(npc);
        var service = CreateService(store);

        service.Unregister(npc);

        Assert.AreEqual(0, await store.CountAsync());
        npc.Received(1).Destroy();
    }

    [TestMethod]
    public async Task Unregister_WhenDestroyFails_StillRemovesTheNpcAndPropagatesTheFailure()
    {
        var store = new NpcStore();
        var npc = CreateNpc();
        var destroyFailure = new InvalidOperationException("npc destruction failed");
        store.Add(npc);
        npc.When(value => value.Destroy()).Do(_ => throw destroyFailure);
        var service = CreateService(store);

        var actual = Assert.ThrowsExactly<InvalidOperationException>(() => service.Unregister(npc));

        Assert.AreSame(destroyFailure, actual);
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
        npc.When(value => value.OnRegistered()).Do(_ =>
        {
            region.Add(npc);
            throw failure;
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

        npc.DidNotReceive().OnRegistered();
        npc.Received(1).Destroy();
    }

    [TestMethod]
    public async Task RegisterAsync_WhenSuccessfulPublishesExactlyOneEntryAndUnregisterRemovesIt()
    {
        var store = new NpcStore();
        var npc = CreateNpc();
        var service = CreateService(store);

        await service.RegisterAsync(npc);
        Assert.AreEqual(1, await store.CountAsync());
        npc.Received(1).OnRegistered();

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
        firstNpc.When(value => value.OnRegistered()).Do(_ =>
        {
            if (++attempts == 1)
            {
                throw failure;
            }
        });
        var service = CreateService(store);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.RegisterAsync(firstNpc));
        Assert.AreEqual(0, await store.CountAsync());

        var secondNpc = CreateNpc();
        await service.RegisterAsync(secondNpc);

        Assert.AreEqual(1, await store.CountAsync());
        secondNpc.Received(1).OnRegistered();
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

        npc.DidNotReceive().OnRegistered();
        Assert.AreEqual(0, await store.CountAsync());
    }

    [TestMethod]
    public async Task UnregisterAsync_WhenDestroyThrows_StillRemovesTheGlobalEntry()
    {
        var store = new NpcStore();
        var npc = CreateNpc();
        await store.AddAsync(npc);
        var destroyFailure = new InvalidOperationException("npc destruction failed");
        npc.When(value => value.Destroy()).Do(_ => throw destroyFailure);
        var service = CreateService(store);

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.UnregisterAsync(npc));

        Assert.AreSame(destroyFailure, actual);
        Assert.AreEqual(0, await store.CountAsync());
        npc.Received(1).Destroy();
    }

    [TestMethod]
    public async Task UnregisterAsync_WhenNpcIsAlreadyDestroyed_StillRemovesTheGlobalEntry()
    {
        var store = new NpcStore();
        var npc = CreateNpc();
        npc.IsDestroyed.Returns(true);
        await store.AddAsync(npc);
        var service = CreateService(store);

        await service.UnregisterAsync(npc);

        Assert.AreEqual(0, await store.CountAsync());
        npc.DidNotReceive().Destroy();
    }

    [TestMethod]
    public async Task UnregisterAsync_WhenDestroyAndRemovalBothFail_PreservesDestroyFailure()
    {
        var store = Substitute.For<INpcStore>();
        var npc = CreateNpc();
        var destroyFailure = new InvalidOperationException("npc destruction failed");
        var removalFailure = new InvalidOperationException("npc removal failed");
        npc.When(value => value.Destroy()).Do(_ => throw destroyFailure);
#pragma warning disable CA2012 // NSubstitute consumes the configured ValueTask.
        store.RemoveAsync(npc).Returns(_ => ValueTask.FromException<bool>(removalFailure));
#pragma warning restore CA2012
        var service = CreateService(store);

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.UnregisterAsync(npc));

        Assert.AreSame(destroyFailure, actual);
        npc.Received(1).Destroy();
        await store.Received(1).RemoveAsync(npc);
    }

    [TestMethod]
    public async Task UnregisterAsync_WhenDestroySucceedsAndRemovalFails_PropagatesRemovalFailure()
    {
        var store = Substitute.For<INpcStore>();
        var npc = CreateNpc();
        var removalFailure = new InvalidOperationException("npc removal failed");
#pragma warning disable CA2012 // NSubstitute consumes the configured ValueTask.
        store.RemoveAsync(npc).Returns(_ => ValueTask.FromException<bool>(removalFailure));
#pragma warning restore CA2012
        var service = CreateService(store);

        var actual = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.UnregisterAsync(npc));

        Assert.AreSame(removalFailure, actual);
        npc.Received(1).Destroy();
        await store.Received(1).RemoveAsync(npc);
    }

    [TestMethod]
    public void Unregister_WhenDestroySucceedsAndRemovalFails_PropagatesRemovalFailure()
    {
        var store = Substitute.For<INpcStore>();
        var npc = CreateNpc();
        var removalFailure = new InvalidOperationException("npc removal failed");
        store.When(value => value.Remove(npc)).Do(_ => throw removalFailure);
        var service = CreateService(store);

        var actual = Assert.ThrowsExactly<InvalidOperationException>(() => service.Unregister(npc));

        Assert.AreSame(removalFailure, actual);
        npc.Received(1).Destroy();
        store.Received(1).Remove(npc);
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
