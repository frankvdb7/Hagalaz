using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Skills.Agility.Courses.Gnome;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Agility.Courses.Gnome;

[TestClass]
public sealed class SignpostTests
{
    [TestMethod]
    public void OnCharacterClick_DisabledTargetDoesNotUseNearbyFallback()
    {
        var target = Substitute.For<IGameObject>();
        var targetHandle = new EntityHandle<IGameObject>(31, 8);
        target.Handle.Returns(targetHandle);
        target.IsDisabled.Returns(true);
        target.Location.Returns(new Location(2480, 3418, 0, 0));

        var entityService = Substitute.For<IEntityService>();
        entityService.TryResolve<IGameObject>(targetHandle, out Arg.Any<IGameObject>()).Returns(callInfo =>
        {
            callInfo[1] = target;
            return true;
        });

        var path = Substitute.For<IPath>();
        path.Successful.Returns(false);
        path.MovedNear.Returns(false);
        var pathFinder = Substitute.For<ISmartPathFinder>();
        pathFinder.Find(Arg.Any<IEntity>(), target, true).Returns(path);
        var pathFinderProvider = Substitute.For<IPathFinderProvider>();
        pathFinderProvider.Smart.Returns(pathFinder);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IEntityService)).Returns(entityService);
        serviceProvider.GetService(typeof(IPathFinderProvider)).Returns(pathFinderProvider);

        var clicker = Substitute.For<ICharacter>();
        clicker.ServiceProvider.Returns(serviceProvider);
        clicker.Location.Returns(new Location(2480, 3418, 0, 0));
        clicker.EventManager.SendEvent(Arg.Any<IEvent>()).Returns(true);
        ITaskItem? queuedTask = null;
        clicker.QueueTask(Arg.Do<ITaskItem>(task => queuedTask = task)).Returns(Substitute.For<IRsTaskHandle>());

        var script = new TestSignpost(Substitute.For<IMovementBuilder>());
        script.Initialize(target);
        script.OnCharacterClick(clicker, GameObjectClickType.Option1Click, false);

        Assert.IsNotNull(queuedTask);
        queuedTask!.Tick();

        Assert.IsFalse(script.InteractionPerformed);
        pathFinder.DidNotReceive().Find(clicker, target, true);
    }

    private sealed class TestSignpost(IMovementBuilder movementBuilder) : Signpost(movementBuilder)
    {
        public bool InteractionPerformed { get; private set; }

        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType) =>
            InteractionPerformed = true;
    }
}
