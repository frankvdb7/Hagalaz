using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Equipment.Degraded;
using Hagalaz.Services.GameWorld.Data;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Equipment;

[TestClass]
public sealed class DegradeTaskLifecycleTests
{
    [TestMethod]
    public void InitialEquipPublication_KeepsCountdownRunning()
    {
        var world = new World();
        var item = world.Equip(EquipmentSlot.Hat, 100);
        var task = world.StartTask(item, 10);

        world.PublishEquipmentChanged(EquipmentSlot.Hat);
        world.Tick(task, 3);

        Assert.IsFalse(task.IsCancelled);
        Assert.AreEqual(7, task.ExecuteDelay);
        Assert.AreEqual(100, item.ExtraData[0]);
    }

    [TestMethod]
    public void UnrelatedEquipmentChange_KeepsCountdownRunning()
    {
        var world = new World();
        var item = world.Equip(EquipmentSlot.Hat, 100);
        var task = world.StartTask(item, 10);
        world.Equip(EquipmentSlot.Amulet, 100);

        world.PublishEquipmentChanged(EquipmentSlot.Amulet);

        Assert.IsFalse(task.IsCancelled);
    }

    [TestMethod]
    public void Unequip_SavesRemainingTicksAndCancelsOnce()
    {
        var world = new World();
        var item = world.Equip(EquipmentSlot.Hat, 100);
        var task = world.StartTask(item, 10);
        world.Tick(task, 4);

        world.Unequip(EquipmentSlot.Hat);
        world.PublishEquipmentChanged(EquipmentSlot.Hat);

        Assert.AreEqual(6, item.ExtraData[0]);
        Assert.IsTrue(task.IsCancelled);
        world.Tick(task, 20);
        world.Character.Equipment.DidNotReceive().RemoveEquippedItem(Arg.Any<IItem>(), Arg.Any<EquipmentSlot>());
        Assert.AreEqual(6, item.ExtraData[0]);
    }

    [TestMethod]
    public void Replacement_SavesRemainingTicksOnDepartedItemOnly()
    {
        var world = new World();
        var old = world.Equip(EquipmentSlot.Hat, 100);
        var task = world.StartTask(old, 10);
        world.Tick(task, 2);

        var replacement = world.Equip(EquipmentSlot.Hat, 100);
        world.PublishEquipmentChanged(EquipmentSlot.Hat);

        Assert.AreEqual(8, old.ExtraData[0]);
        Assert.AreEqual(100, replacement.ExtraData[0]);
        Assert.IsTrue(task.IsCancelled);
    }

    [TestMethod]
    public void Detach_SavesRemainingTicksSynchronouslyBeforeSnapshot()
    {
        var world = new World();
        var item = world.Equip(EquipmentSlot.Hat, 1500);
        var task = world.StartTask(item, 1500);
        world.Tick(task, 600);

        world.Bus.SendEvent(new CharacterDetachingEvent(world.Character));

        Assert.AreEqual(900, item.ExtraData[0]);
    }

    [TestMethod]
    public void DestroyAfterDetach_DoesNotRewriteSavedTicks()
    {
        var world = new World();
        var item = world.Equip(EquipmentSlot.Hat, 1500);
        var task = world.StartTask(item, 1500);
        world.Tick(task, 600);
        world.Bus.SendEvent(new CharacterDetachingEvent(world.Character));
        world.Tick(task, 10);

        world.Bus.SendEvent(new CreatureDestroyedEvent(world.Character));

        Assert.AreEqual(900, item.ExtraData[0]);
        Assert.IsTrue(task.IsCancelled);
    }

    [TestMethod]
    public void Expiry_RemovesEquippedItemOnce()
    {
        var world = new World();
        var item = world.Equip(EquipmentSlot.Hat, 100);
        var task = world.StartTask(item, 3);

        world.Tick(task, 10);

        world.Character.Equipment.Received(1).RemoveEquippedItem(item, EquipmentSlot.Hat);
    }

    private sealed class World
    {
        private readonly Dictionary<EquipmentSlot, IItem> _equipped = [];

        public InMemoryEventBus Bus { get; } = new();
        public ICharacter Character { get; } = Substitute.For<ICharacter>();

        public World()
        {
            var equipment = Substitute.For<IEquipmentContainer>();
            equipment.GetInstanceSlot(Arg.Any<IItem>()).Returns(call =>
                _equipped.FirstOrDefault(pair => ReferenceEquals(pair.Value, call.Arg<IItem>())) is { Value: not null } found
                    ? found.Key
                    : EquipmentSlot.NoSlot);
            equipment[Arg.Any<EquipmentSlot>()].Returns(call => _equipped.GetValueOrDefault(call.Arg<EquipmentSlot>()));
            Character.Equipment.Returns(equipment);
            Character.RegisterEventHandler(Arg.Any<EventHappened<EquipmentChangedEvent>>())
                .Returns(call => Bus.Listen(call.Arg<EventHappened<EquipmentChangedEvent>>()));
            Character.RegisterEventHandler(Arg.Any<EventHappened<CharacterDetachingEvent>>())
                .Returns(call => Bus.Listen(call.Arg<EventHappened<CharacterDetachingEvent>>()));
            Character.RegisterEventHandler(Arg.Any<EventHappened<CreatureDestroyedEvent>>())
                .Returns(call => Bus.Listen(call.Arg<EventHappened<CreatureDestroyedEvent>>()));
            Character.UnregisterEventHandler<EquipmentChangedEvent>(Arg.Any<EventHappened>())
                .Returns(call => Bus.StopListen<EquipmentChangedEvent>(call.Arg<EventHappened>()));
            Character.UnregisterEventHandler<CharacterDetachingEvent>(Arg.Any<EventHappened>())
                .Returns(call => Bus.StopListen<CharacterDetachingEvent>(call.Arg<EventHappened>()));
            Character.UnregisterEventHandler<CreatureDestroyedEvent>(Arg.Any<EventHappened>())
                .Returns(call => Bus.StopListen<CreatureDestroyedEvent>(call.Arg<EventHappened>()));
            equipment.RemoveEquippedItem(Arg.Any<IItem>(), Arg.Any<EquipmentSlot>()).Returns(1);
        }

        public IItem Equip(EquipmentSlot slot, long ticks)
        {
            var item = Substitute.For<IItem>();
            item.ExtraData.Returns(new[] { ticks });
            item.Equals(Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call => ReferenceEquals(item, call.Arg<IItem>()));
            _equipped[slot] = item;
            return item;
        }

        public void Unequip(EquipmentSlot slot) => _equipped.Remove(slot);

        public RsTask StartTask(IItem item, int ticks) => new DegradeTask(Character, item, ticks);

        public void PublishEquipmentChanged(params EquipmentSlot[] slots) =>
            Bus.SendEvent(new EquipmentChangedEvent(Character, [.. slots]));

        public void Tick(RsTask task, int ticks)
        {
            for (var i = 0; i < ticks; i++) task.Tick();
        }
    }
}
