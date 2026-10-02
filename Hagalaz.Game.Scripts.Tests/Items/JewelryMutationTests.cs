using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Items.Jewelry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Items;

[TestClass]
public sealed class JewelryMutationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ChargedGlory_ReplacesExactItemInstanceInInventoryOrEquipment(bool equipped)
    {
        var inventory = new ComposedTestContainer(2);
        var equipment = Substitute.For<IEquipmentContainer>();
        equipment.Capacity.Returns(14);
        var jewelry = Substitute.For<IItem>();
        jewelry.Id.Returns(1712);
        jewelry.Count.Returns(1);
        jewelry.Name.Returns("Amulet of glory (6)");
        if (equipped)
        {
            equipment[2].Returns(jewelry);
            equipment.GetEnumerator().Returns(_ => new IItem?[] { null, null, jewelry }.AsEnumerable().GetEnumerator());
        }
        else
        {
            inventory.SetItem(0, jewelry);
        }

        var character = CreateCharacter(inventory, equipment);

        Jewelry.TeleportAmuletOfGlory(character, jewelry, equipped, Jewelry.GloryTeleports[0]);

        if (equipped)
        {
            equipment.Received(1).TryReplaceEquippedItem(EquipmentSlot.Amulet, jewelry,
                Arg.Is<IItem>(item => item.Id == 1710 && item.Count == 1));
        }
        else
        {
            Assert.AreEqual(1710, inventory.Items[0]!.Id);
            Assert.AreEqual(1, inventory.Items[0]!.Count);
        }
    }

    [TestMethod]
    public void DepletedSlayingRingInInventory_IsRemovedAndReportsDepletion()
    {
        var inventory = new ComposedTestContainer(2);
        var ring = new JewelryTestItem(2150, 1, "Ring of slaying (1)");
        inventory.SetItem(0, ring);
        var character = CreateCharacter(inventory, Substitute.For<IEquipmentContainer>());

        Jewelry.TeleportRingOfSlaying(character, ring, equipment: false, Jewelry.RingOfSlayingTeleports[0]);

        Assert.AreEqual(0, inventory.Items.GetCountById(ring.Id));
        character.Received().SendChatMessage(Arg.Is<string>(message => message.Contains("depleted")));
    }

    [TestMethod]
    public void DepletedEquippedSlayingRing_IsRemovedFromItsExactEquipmentSlot()
    {
        var ring = new JewelryTestItem(2150, 1, "Ring of slaying (1)");
        var inventory = new ComposedTestContainer(2);
        var equipment = Substitute.For<IEquipmentContainer>();
        equipment.Capacity.Returns(14);
        var slot = (int)EquipmentSlot.Ring;
        var items = new IItem?[equipment.Capacity];
        items[slot] = ring;
        equipment[slot].Returns(ring);
        equipment.GetEnumerator().Returns(_ => items.AsEnumerable().GetEnumerator());
        equipment.RemoveEquippedItem(ring, EquipmentSlot.Ring).Returns(_ =>
        {
            items[slot] = null;
            return 1;
        });
        var character = CreateCharacter(inventory, equipment);

        Jewelry.TeleportRingOfSlaying(character, ring, equipment: true, Jewelry.RingOfSlayingTeleports[0]);

        equipment.Received(1).RemoveEquippedItem(ring, EquipmentSlot.Ring);
        Assert.IsNull(items[slot]);
        character.Received(1).SendChatMessage("Your Ring of slaying  has been depleted of all its charges.");
        equipment.DidNotReceive().TryReplaceEquippedItem(Arg.Any<EquipmentSlot>(), Arg.Any<IItem>(), Arg.Any<IItem>());
    }

    [TestMethod]
    public void DepletedEquippedSlayingRing_StaleInstanceDoesNotRemoveOrReportSuccess()
    {
        var current = new JewelryTestItem(2150, 1, "Ring of slaying (1)");
        var stale = new JewelryTestItem(2150, 1, "Ring of slaying (1)");
        var inventory = new ComposedTestContainer(2);
        var equipment = Substitute.For<IEquipmentContainer>();
        equipment.Capacity.Returns(14);
        var slot = (int)EquipmentSlot.Ring;
        var items = new IItem?[equipment.Capacity];
        items[slot] = current;
        equipment[slot].Returns(current);
        equipment.GetEnumerator().Returns(_ => items.AsEnumerable().GetEnumerator());
        var character = CreateCharacter(inventory, equipment);

        Jewelry.TeleportRingOfSlaying(character, stale, equipment: true, Jewelry.RingOfSlayingTeleports[0]);

        Assert.AreSame(current, items[slot]);
        equipment.DidNotReceive().RemoveEquippedItem(Arg.Any<IItem>(), Arg.Any<EquipmentSlot>());
        character.DidNotReceive().SendChatMessage(Arg.Is<string>(message => message.Contains("depleted")));
    }

    [TestMethod]
    public void StaleJewelryInstance_DoesNotReplaceAnotherItemWithTheSameId()
    {
        var inventory = new ComposedTestContainer(2);
        var current = Substitute.For<IItem>();
        current.Id.Returns(1712);
        current.Count.Returns(1);
        current.Name.Returns("Amulet of glory (6)");
        var stale = Substitute.For<IItem>();
        stale.Id.Returns(1712);
        stale.Count.Returns(1);
        stale.Name.Returns("Amulet of glory (6)");
        inventory.SetItem(0, current);
        var character = CreateCharacter(inventory, Substitute.For<IEquipmentContainer>());

        Jewelry.TeleportAmuletOfGlory(character, stale, equipment: false, Jewelry.GloryTeleports[0]);

        Assert.AreSame(current, inventory.Items[0]);
    }

    private static ICharacter CreateCharacter(ComposedTestContainer inventory, IEquipmentContainer equipment)
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IItemBuilder)).Returns(new TestItemBuilder(ComposedTestContainer.CreateTestItem));
        provider.GetService(typeof(IMapRegionService)).Returns(Substitute.For<IMapRegionService>());
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        character.Equipment.Returns(equipment);
        character.ServiceProvider.Returns(provider);
        character.Movement.Returns(Substitute.For<IMovement>());
        return character;
    }

    private sealed class JewelryTestItem(int id, int count, string name) : IItem
    {
        public int Id { get; } = id;
        public long[] ExtraData => [];
        public int Count { get; set; } = count;
        public string Name { get; } = name;
        public IItemDefinition ItemDefinition { get; } = Substitute.For<IItemDefinition>();
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript { get; } = Substitute.For<IItemScript>();
        public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
        public IItem Clone() => new JewelryTestItem(Id, Count, Name);
        public IItem Clone(int newCount) => new JewelryTestItem(Id, newCount, Name);
        public bool Equals(IItem otherItem, bool ignoreCount = true) => otherItem is not null && Id == otherItem.Id &&
            (ignoreCount || Count == otherItem.Count) && ExtraData.SequenceEqual(otherItem.ExtraData);
        public string? SerializeExtraData() => null;
    }
}
