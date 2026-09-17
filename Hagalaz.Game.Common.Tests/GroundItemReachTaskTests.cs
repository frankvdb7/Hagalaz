using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Common.Tests;

[TestClass]
public sealed class GroundItemReachTaskTests
{
    [TestMethod]
    public void GroundItemReachTask_TargetRemovedBeforeTick_Fails()
    {
        var (reacher, entityService, pathFinder) = CreateFixture();
        var target = CreateTarget(new EntityHandle<IGroundItem>(22, 4));
        var isRegistered = true;
        entityService.TryResolve<IGroundItem>(target.Handle, out Arg.Any<IGroundItem>()).Returns(callInfo =>
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
        var task = new GroundItemReachTask(reacher, target.Handle, value => result = value);

        isRegistered = false;
        task.Tick();

        Assert.IsFalse(result);
        Assert.IsTrue(task.IsCancelled);
        pathFinder.DidNotReceive().Find(reacher, target, true);
    }

    [TestMethod]
    public void GroundItemReachTask_StaleHandleDoesNotRetargetReplacement()
    {
        var (reacher, entityService, pathFinder) = CreateFixture();
        var target = CreateTarget(new EntityHandle<IGroundItem>(23, 7));
        var replacement = CreateTarget(new EntityHandle<IGroundItem>(target.Handle.Slot, target.Handle.Generation + 1));
        var isRegistered = true;
        entityService.TryResolve<IGroundItem>(target.Handle, out Arg.Any<IGroundItem>()).Returns(callInfo =>
        {
            if (isRegistered)
            {
                callInfo[1] = target;
                return true;
            }

            callInfo[1] = null;
            return false;
        });
        var task = new GroundItemReachTask(reacher, target.Handle, value => { });

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

    private static IGroundItem CreateTarget(EntityHandle<IGroundItem> handle)
    {
        var target = Substitute.For<IGroundItem>();
        target.Handle.Returns(handle);
        target.Location.Returns(new Location(3200, 3200, 0, 0));
        return target;
    }
}
