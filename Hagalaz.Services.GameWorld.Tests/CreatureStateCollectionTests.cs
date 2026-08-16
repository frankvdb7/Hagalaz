using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Services.GameWorld.Model.Creatures;
using Hagalaz.Services.GameWorld.Providers;
using Hagalaz.Services.GameWorld.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CreatureStateCollectionTests
{
    [TestMethod]
    public void PassiveState_RemainsActiveAcrossGameTicks()
    {
        var collection = CreateCollection();
        collection.Add(new BowEquippedState());

        collection.ProcessTick();
        collection.ProcessTick();

        Assert.IsTrue(collection.Has(typeof(BowEquippedState)));
    }

    [TestMethod]
    public void TimedState_ExpiresOnExpectedTick()
    {
        var collection = CreateCollection();
        collection.Add(new TestTimedState { TicksLeft = 2 });

        collection.ProcessTick();
        Assert.IsTrue(collection.Has(typeof(TestTimedState)));

        collection.ProcessTick();
        Assert.IsFalse(collection.Has(typeof(TestTimedState)));
    }

    [TestMethod]
    public void TimedState_UsesKeepLongestDurationPolicy()
    {
        var collection = CreateCollection();
        var existing = new KeepLongestTimedState { TicksLeft = 5 };
        collection.Add(existing);

        var shorter = new KeepLongestTimedState { TicksLeft = 2 };
        collection.Add(shorter);
        Assert.AreSame(existing, collection.States.Single());
        Assert.AreEqual(0, existing.RemovedCount);

        var longer = new KeepLongestTimedState { TicksLeft = 6 };
        collection.Add(longer);
        Assert.AreSame(longer, collection.States.Single());
        Assert.AreEqual(1, existing.RemovedCount);
    }

    [TestMethod]
    public void TimedStateWithoutKeepLongestCapability_UsesKeepExistingDefault()
    {
        var collection = CreateCollection();
        var existing = new TestTimedState { TicksLeft = 5 };
        collection.Add(existing);

        collection.Add(new TestTimedState { TicksLeft = 10 });

        Assert.AreSame(existing, collection.States.Single());
    }

    [TestMethod]
    public void RejectedDuplicate_DoesNotRaiseRemovalCallback()
    {
        var collection = CreateCollection();
        var existing = new LifecycleState();
        collection.Add(existing);

        collection.Add(new LifecycleState());

        Assert.AreEqual(0, existing.RemovedCount);
    }

    [TestMethod]
    public void FreezeAndStaffOfLightUseTimedAndLifecycleCapabilities()
    {
        var collection = CreateCollection();
        collection.Add(new FrozenState { TicksLeft = 1 });
        var removedCount = 0;
        collection.Add(new StaffOfLightSpecialEffectState
        {
            TicksLeft = 1,
            OnRemovedCallback = () => removedCount++
        });

        collection.ProcessTick();

        Assert.IsFalse(collection.Has(typeof(FrozenState)));
        Assert.IsFalse(collection.Has(typeof(StaffOfLightSpecialEffectState)));
        Assert.AreEqual(1, removedCount);
    }

    [TestMethod]
    public void DurableStateClassification_IncludesCharacterOwnedMarkersOnly()
    {
        Assert.IsInstanceOfType<IPersistentState>(new DefaultSkulledState());
        Assert.IsInstanceOfType<IPersistentState>(new HasGodWarsHoleRopeState());
        Assert.IsInstanceOfType<IPersistentState>(new HasSaradominFirstRockRopeState());
        Assert.IsInstanceOfType<IPersistentState>(new HasSaradominLastRockRopeState());
        Assert.IsInstanceOfType<IPersistentState>(new LodestoneActivatedState());

        Assert.IsNotInstanceOfType<IPersistentState>(new BowEquippedState());
        Assert.IsNotInstanceOfType<IPersistentState>(new FrozenState());
        Assert.IsNotInstanceOfType<IPersistentState>(new GnomeCourseTreeState());
    }

    private static CreatureStateCollection CreateCollection() => new(Substitute.For<ICreature>());

    private sealed class TestPassiveState : State
    {
    }

    private sealed class TestTimedState : State, ITimedState
    {
        public int TicksLeft { get; set; }
    }

    private sealed class KeepLongestTimedState : TimedState, IKeepLongestDurationState, IStateLifecycle
    {
        public int RemovedCount { get; private set; }

        public void OnRemoved(ICreature creature) => RemovedCount++;
    }

    private sealed class LifecycleState : State, IStateLifecycle
    {
        public int RemovedCount { get; private set; }

        public void OnRemoved(ICreature creature) => RemovedCount++;
    }
}

[TestClass]
public sealed class StateProviderTests
{
    [TestMethod]
    public async Task TryCreateState_ReturnsFalseForUnknownId()
    {
        using var provider = BuildProvider(new TestStateFactory(("known", typeof(RegisteredState))));
        var stateProvider = new StateProvider(provider, NullLogger<StateProvider>.Instance);
        await stateProvider.LoadAsync();
        using var scope = provider.CreateScope();
        var stateService = new StateService(stateProvider, scope.ServiceProvider);

        Assert.IsFalse(stateService.TryCreateState("missing", out var state));
        Assert.IsNull(state);
        Assert.IsTrue(stateService.TryCreateState("known", out state));
        Assert.IsInstanceOfType<RegisteredState>(state);
        Assert.IsTrue(stateService.TryGetStateId(state, out var id));
        Assert.AreEqual("known", id);
    }

    [TestMethod]
    public async Task LoadAsync_RejectsDuplicateIds()
    {
        using var provider = BuildProvider(
            new TestStateFactory(("duplicate", typeof(RegisteredState))),
            new TestStateFactory(("duplicate", typeof(SecondRegisteredState))));
        var stateProvider = new StateProvider(provider, NullLogger<StateProvider>.Instance);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => stateProvider.LoadAsync());
    }

    [TestMethod]
    public async Task LoadAsync_SkipsNonPersistentStates()
    {
        using var provider = BuildProvider(
            new TestStateFactory(("runtime-only", typeof(RuntimeOnlyState))),
            new TestStateFactory(("persistent", typeof(RegisteredState))));
        var stateProvider = new StateProvider(provider, NullLogger<StateProvider>.Instance);

        await stateProvider.LoadAsync();
        using var scope = provider.CreateScope();
        var stateService = new StateService(stateProvider, scope.ServiceProvider);

        Assert.IsFalse(stateService.TryCreateState("runtime-only", out _));
        Assert.IsTrue(stateService.TryCreateState("persistent", out var state));
        Assert.IsInstanceOfType<RegisteredState>(state);
    }

    [TestMethod]
    public async Task StateService_ActivatesStateFromCharacterScope()
    {
        using var provider = BuildProvider(new TestStateFactory(("scoped", typeof(ScopedDependentState))));
        var stateProvider = new StateProvider(provider, NullLogger<StateProvider>.Instance);
        await stateProvider.LoadAsync();

        using var scope = provider.CreateScope();
        var stateService = new StateService(stateProvider, scope.ServiceProvider);

        Assert.IsTrue(stateService.TryCreateState("scoped", out var state));
        var scopedState = Assert.IsInstanceOfType<ScopedDependentState>(state);
        Assert.AreSame(scope.ServiceProvider.GetRequiredService<ScopedStateDependency>(), scopedState.Dependency);
    }

    [TestMethod]
    public async Task LoadAsync_RejectsPersistentStatesWithoutStableMetadata()
    {
        using var provider = BuildProvider(new TestStateFactory(("missing-metadata", typeof(MissingMetadataState))));
        var stateProvider = new StateProvider(provider, NullLogger<StateProvider>.Instance);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => stateProvider.LoadAsync());

        StringAssert.Contains(exception.Message, nameof(MissingMetadataState));
        StringAssert.Contains(exception.Message, nameof(StateMetaDataAttribute));
    }

    private static ServiceProvider BuildProvider(params IStateFactory[] factories)
    {
        var services = new ServiceCollection();
        services.AddTransient<RegisteredState>();
        services.AddTransient<SecondRegisteredState>();
        services.AddTransient<MissingMetadataState>();
        services.AddScoped<ScopedStateDependency>();
        services.AddTransient<ScopedDependentState>();
        foreach (var factory in factories)
        {
            services.AddSingleton(typeof(IStateFactory), factory);
        }

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private sealed class TestStateFactory : IStateFactory
    {
        private readonly (string Id, Type Type) _state;

        public TestStateFactory((string Id, Type Type) state) => _state = state;

        public async IAsyncEnumerable<(string stateId, Type scriptType)> GetStates()
        {
            await Task.CompletedTask;
            yield return (_state.Id, _state.Type);
        }
    }

    [StateMetaData("registered-state")]
    private sealed class RegisteredState : State, IPersistentState
    {
    }

    private sealed class RuntimeOnlyState : State
    {
    }

    private sealed class MissingMetadataState : State, IPersistentState
    {
    }

    [StateMetaData("scoped-dependent-state")]
    private sealed class ScopedDependentState : State, IPersistentState
    {
        public ScopedDependentState(ScopedStateDependency dependency) => Dependency = dependency;

        public ScopedStateDependency Dependency { get; }
    }

    private sealed class ScopedStateDependency
    {
    }

    [StateMetaData("second-registered-state")]
    private sealed class SecondRegisteredState : State, IPersistentState
    {
    }
}
