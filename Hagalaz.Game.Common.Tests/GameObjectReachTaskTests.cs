using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Common.Tests;

[TestClass]
public sealed class GameObjectReachTaskTests
{
    [TestMethod]
    public void GameObjectReachTask_TargetRemovedBeforeTick_Fails()
    {
        var (reacher, entityService, pathFinder) = CreateFixture();
        var target = CreateTarget(new EntityHandle(12, 4));
        var isRegistered = true;
        entityService.TryResolve<IGameObject>(target.Handle, out Arg.Any<IGameObject>()).Returns(callInfo =>
        {
            if (isRegistered)
            {
                callInfo[1] = target;
                return true;
            }

            callInfo[1] = null;
            return false;
        });
        var result = true;
        var task = new GameObjectReachTask(reacher, target.Handle, value => result = value);

        isRegistered = false;
        task.Tick();

        Assert.IsFalse(result);
        Assert.IsTrue(task.IsCancelled);
        pathFinder.DidNotReceive().Find(reacher, target, true);
    }

    [TestMethod]
    public void GameObjectReachTask_DisabledStaticTarget_Fails()
    {
        var (reacher, entityService, pathFinder) = CreateFixture();
        var target = CreateTarget(new EntityHandle(13, 5));
        target.IsDisabled.Returns(true);
        entityService.TryResolve<IGameObject>(target.Handle, out Arg.Any<IGameObject>()).Returns(callInfo =>
        {
            callInfo[1] = target;
            return true;
        });

        Assert.IsTrue(entityService.TryResolve<IGameObject>(target.Handle, out var resolved));
        Assert.AreSame(target, resolved);

        var result = true;
        var task = new GameObjectReachTask(reacher, target.Handle, value => result = value);
        task.Tick();

        Assert.IsFalse(result);
        Assert.IsTrue(task.IsCancelled);
        pathFinder.DidNotReceive().Find(reacher, target, true);
    }

    [TestMethod]
    public void GameObjectReachTask_StaleHandleDoesNotRetargetReplacement()
    {
        var (reacher, entityService, pathFinder) = CreateFixture();
        var target = CreateTarget(new EntityHandle(14, 6));
        var replacement = CreateTarget(new EntityHandle(target.Handle.Slot, target.Handle.Generation + 1));
        var isRegistered = true;
        entityService.TryResolve<IGameObject>(target.Handle, out Arg.Any<IGameObject>()).Returns(callInfo =>
        {
            if (isRegistered)
            {
                callInfo[1] = target;
                return true;
            }

            callInfo[1] = null;
            return false;
        });
        var task = new GameObjectReachTask(reacher, target.Handle, value => { });

        isRegistered = false;
        Assert.AreNotEqual(target.Handle.Generation, replacement.Handle.Generation);
        task.Tick();

        Assert.IsTrue(task.IsCancelled);
        pathFinder.DidNotReceive().Find(reacher, replacement, true);
    }

    private static (ICreature Reacher, IEntityService EntityService, ISmartPathFinder PathFinder) CreateFixture()
    {
        var reacher = Substitute.For<ICreature>();
        var entityService = Substitute.For<IEntityService>();
        var pathFinder = Substitute.For<ISmartPathFinder>();
        var pathFinderProvider = Substitute.For<IPathFinderProvider>();
        var serviceProvider = Substitute.For<IServiceProvider>();

        pathFinderProvider.Smart.Returns(pathFinder);
        serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
        serviceProvider.GetService(typeof(IPathFinderProvider)).Returns(pathFinderProvider);
        reacher.ServiceProvider.Returns(serviceProvider);
        reacher.Viewport.InBounds(Arg.Any<ILocation>()).Returns(true);
        reacher.Movement.Locked.Returns(false);

        return (reacher, entityService, pathFinder);
    }

    private static IGameObject CreateTarget(EntityHandle handle)
    {
        var target = Substitute.For<IGameObject>();
        target.Handle.Returns(handle);
        target.Location.Returns(new Location(3200, 3200, 0, 0));
        target.Size.Returns(1);
        target.IsDisabled.Returns(false);
        return target;
    }
}
