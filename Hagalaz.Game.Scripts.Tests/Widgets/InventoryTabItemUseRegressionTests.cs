using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Scripts.Widgets.Tabs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets;

[TestClass]
public sealed class InventoryTabItemUseRegressionTests
{
    [TestMethod]
    public void UseOnHandlers_RejectStaleItemsAndQueueDomainSpecificReachTasks()
    {
        var inventory = new ComposedTestContainer(2);
        var item = ComposedTestContainer.CreateTestItem(100);
        inventory.SetItem(0, item);
        var entityService = Substitute.For<IEntityService>();
        var pathFinderProvider = Substitute.For<IPathFinderProvider>();
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IEntityService)).Returns(entityService);
        provider.GetService(typeof(IPathFinderProvider)).Returns(pathFinderProvider);
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        character.ServiceProvider.Returns(provider);
        character.QueueTask(Arg.Any<ITaskItem>()).Returns(Substitute.For<IRsTaskHandle>());
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>();
        accessor.Context.Returns(context);
        var widget = Substitute.For<IWidget>();
        OnComponentUsedOnGameObject? objectHandler = null;
        OnComponentUsedOnGroundItem? groundItemHandler = null;
        OnComponentUsedOnCreature? creatureHandler = null;
        widget.When(x => x.AttachUseOnObjectHandler(0, Arg.Any<OnComponentUsedOnGameObject>()))
            .Do(call => objectHandler = call.ArgAt<OnComponentUsedOnGameObject>(1));
        widget.When(x => x.AttachUseOnGroundItemHandler(0, Arg.Any<OnComponentUsedOnGroundItem>()))
            .Do(call => groundItemHandler = call.ArgAt<OnComponentUsedOnGroundItem>(1));
        widget.When(x => x.AttachUseOnCreatureHandler(0, Arg.Any<OnComponentUsedOnCreature>()))
            .Do(call => creatureHandler = call.ArgAt<OnComponentUsedOnCreature>(1));
        var tab = new InventoryTab(accessor);
        tab.Initialize(widget);
        tab.OnOpen();

        Assert.IsFalse(objectHandler!(0, Substitute.For<IGameObject>(), false, 101, 0));
        Assert.IsFalse(groundItemHandler!(0, Substitute.For<IGroundItem>(), false, 101, 0));
        Assert.IsFalse(creatureHandler!(0, Substitute.For<ICreature>(), false, 101, 0));
        character.DidNotReceive().QueueTask(Arg.Any<ITaskItem>());

        ITaskItem? queuedTask = null;
        character.QueueTask(Arg.Do<ITaskItem>(task => queuedTask = task)).Returns(Substitute.For<IRsTaskHandle>());
        Assert.IsTrue(objectHandler(0, Substitute.For<IGameObject>(), false, 100, 0));
        Assert.IsInstanceOfType<GameObjectReachTask>(queuedTask);
        Assert.IsTrue(groundItemHandler(0, Substitute.For<IGroundItem>(), false, 100, 0));
        Assert.IsInstanceOfType<GroundItemReachTask>(queuedTask);
        Assert.IsTrue(creatureHandler(0, Substitute.For<ICreature>(), false, 100, 0));
        Assert.IsInstanceOfType<CreatureReachTask>(queuedTask);
    }
}
