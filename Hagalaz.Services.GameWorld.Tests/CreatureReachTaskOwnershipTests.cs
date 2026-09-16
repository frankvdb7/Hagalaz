using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Services;
using Hagalaz.Services.GameWorld.Store;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class CreatureReachTaskOwnershipTests
{
    [TestMethod]
    public async Task Tick_WhenCharacterTargetIsStillOwned_UsesTheCurrentTarget()
    {
        var store = CreateCharacterStore();
        var target = CreateCharacter(1);
        Assert.IsTrue(await store.AddAsync(target));

        var pathFinder = Substitute.For<ISmartPathFinder>();
        using var provider = CreateProvider(pathFinder, characterStore: store);
        var entityStore = provider.GetRequiredService<IEntityStore>();
        entityStore.Add(target);
        var (reacher, visibleCreatures) = CreateReacher(provider);
        visibleCreatures.Add(target);
        var result = false;
        var reachedPath = CreateReachedPath();
        pathFinder.Find(reacher, target, true).Returns(reachedPath);
        var task = new CreatureReachTask(reacher, target, value => result = value);

        task.Tick();

        Assert.IsTrue(result);
        pathFinder.Received(1).Find(reacher, target, true);
    }

    [TestMethod]
    public async Task Tick_WhenCharacterTargetWasRemoved_FailsWithoutPathfinding()
    {
        var store = CreateCharacterStore();
        var target = CreateCharacter(1);
        Assert.IsTrue(await store.AddAsync(target));

        var pathFinder = Substitute.For<ISmartPathFinder>();
        using var provider = CreateProvider(pathFinder, characterStore: store);
        var entityStore = provider.GetRequiredService<IEntityStore>();
        entityStore.Add(target);
        var (reacher, visibleCreatures) = CreateReacher(provider);
        visibleCreatures.Add(target);
        var result = true;
        var task = new CreatureReachTask(reacher, target, value => result = value);

        Assert.IsTrue(store.Remove(target));
        Assert.IsTrue(entityStore.Remove(target));
        task.Tick();

        Assert.IsFalse(result);
        Assert.IsTrue(task.IsCancelled);
        pathFinder.DidNotReceive().Find(reacher, target, true);
    }

    [TestMethod]
    public async Task Tick_WhenCharacterSlotWasReused_DoesNotReachReplacement()
    {
        var store = CreateCharacterStore();
        var target = CreateCharacter(1);
        var replacement = CreateCharacter(2);
        Assert.IsTrue(await store.AddAsync(target));

        var pathFinder = Substitute.For<ISmartPathFinder>();
        using var provider = CreateProvider(pathFinder, characterStore: store);
        var entityStore = provider.GetRequiredService<IEntityStore>();
        entityStore.Add(target);
        var (reacher, visibleCreatures) = CreateReacher(provider);
        visibleCreatures.Add(replacement);
        var result = true;
        var task = new CreatureReachTask(reacher, target, value => result = value);

        Assert.IsTrue(store.Remove(target));
        Assert.IsTrue(entityStore.Remove(target));
        Assert.IsTrue(await store.AddAsync(replacement));
        entityStore.Add(replacement);
        Assert.AreEqual(target.Index, replacement.Index);
        task.Tick();

        Assert.IsFalse(result);
        Assert.IsTrue(task.IsCancelled);
        pathFinder.DidNotReceive().Find(reacher, target, true);
    }

    [TestMethod]
    public async Task Tick_WhenNpcTargetWasRemoved_FailsWithoutPathfinding()
    {
        var store = new NpcStore();
        var target = EntityTestFactory.Create<INpc>();
        Assert.IsTrue(await store.AddAsync(target));

        var pathFinder = Substitute.For<ISmartPathFinder>();
        using var provider = CreateProvider(pathFinder, npcStore: store);
        var entityStore = provider.GetRequiredService<IEntityStore>();
        entityStore.Add(target);
        var (reacher, visibleCreatures) = CreateReacher(provider);
        visibleCreatures.Add(target);
        var result = true;
        var task = new CreatureReachTask(reacher, target, value => result = value);

        Assert.IsTrue(store.Remove(target));
        Assert.IsTrue(entityStore.Remove(target));
        task.Tick();

        Assert.IsFalse(result);
        Assert.IsTrue(task.IsCancelled);
        pathFinder.DidNotReceive().Find(reacher, target, true);
    }

    [TestMethod]
    public async Task Tick_WhenNpcSlotWasReused_DoesNotReachReplacement()
    {
        var store = new NpcStore();
        var target = EntityTestFactory.Create<INpc>();
        var replacement = EntityTestFactory.Create<INpc>();
        Assert.IsTrue(await store.AddAsync(target));

        var pathFinder = Substitute.For<ISmartPathFinder>();
        using var provider = CreateProvider(pathFinder, npcStore: store);
        var entityStore = provider.GetRequiredService<IEntityStore>();
        entityStore.Add(target);
        var (reacher, visibleCreatures) = CreateReacher(provider);
        visibleCreatures.Add(replacement);
        var result = true;
        var task = new CreatureReachTask(reacher, target, value => result = value);

        Assert.IsTrue(store.Remove(target));
        Assert.IsTrue(entityStore.Remove(target));
        Assert.IsTrue(await store.AddAsync(replacement));
        entityStore.Add(replacement);
        Assert.AreEqual(target.Index, replacement.Index);
        task.Tick();

        Assert.IsFalse(result);
        Assert.IsTrue(task.IsCancelled);
        pathFinder.DidNotReceive().Find(reacher, target, true);
    }

    private static ICharacter CreateCharacter(uint masterId)
    {
        var character = EntityTestFactory.Create<ICharacter>();
        character.MasterId.Returns(masterId);
        character.Size.Returns(1);
        character.Location.Returns(Location.Create(3200, 3200));
        return character;
    }

    private static IPath CreateReachedPath()
    {
        var path = Substitute.For<IPath>();
        path.Successful.Returns(true);
        path.ReachedDestination.Returns(true);
        return path;
    }

    private static ServiceProvider CreateProvider(
        ISmartPathFinder pathFinder,
        ICharacterStore? characterStore = null,
        INpcStore? npcStore = null)
    {
        var pathFinderProvider = Substitute.For<IPathFinderProvider>();
        pathFinderProvider.Smart.Returns(pathFinder);
        var services = new ServiceCollection()
            .AddSingleton<IPathFinderProvider>(pathFinderProvider);
        var entityStore = new EntityStore();
        services.AddSingleton<IEntityStore>(entityStore);
        services.AddSingleton<IEntityService>(new EntityService(entityStore));
        if (characterStore is not null)
        {
            services.AddSingleton(characterStore);
        }

        if (npcStore is not null)
        {
            services.AddSingleton(npcStore);
        }

        return services.BuildServiceProvider();
    }

    private static (ICreature Reacher, List<ICreature> VisibleCreatures) CreateReacher(IServiceProvider provider)
    {
        var reacher = Substitute.For<ICreature>();
        var viewport = Substitute.For<IViewport>();
        var movement = Substitute.For<IMovement>();
        var visibleCreatures = new List<ICreature>();
        reacher.ServiceProvider.Returns(provider);
        reacher.Viewport.Returns(viewport);
        reacher.Movement.Returns(movement);
        viewport.VisibleCreatures.Returns(visibleCreatures);
        return (reacher, visibleCreatures);
    }

    private static CharacterStore CreateCharacterStore() => new(Options.Create(new GameServerOptions
    {
        ClientRevision = 1,
        ClientRevisionPatch = 0,
        AuthenticationToken = "test"
    }));
}
