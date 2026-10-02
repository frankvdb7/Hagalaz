using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class MagicRuneRequirementsTests
{
    [TestMethod]
    public void CheckRunes_WithInfiniteRuneWeapon_SkipsItsRuneAndRetainsOtherRequirements()
    {
        var inventory = new ItemContainer(StorageType.Normal, 4);
        inventory.Add(new RuneItem((int)RuneType.Earth, 1));
        var character = CreateCharacter(inventory, new RuneItem((int)StaffType.AirStaff, 1));
        var magic = new Magic(character, Substitute.For<IItemService>());
        var requirements = new[] { 1, 1 };

        var canCast = magic.CheckRunes([RuneType.Air, RuneType.Earth], requirements);

        Assert.IsTrue(canCast);
        CollectionAssert.AreEqual(new[] { 1, 1 }, requirements);
    }

    [TestMethod]
    public void RemoveRunes_WithInfiniteRuneWeapon_RemovesOnlyNormalRequirementWithoutMutatingInput()
    {
        var inventory = new ItemContainer(StorageType.Normal, 4);
        inventory.Add(new RuneItem((int)RuneType.Earth, 2));
        var character = CreateCharacter(inventory, new RuneItem((int)StaffType.AirStaff, 1));
        var magic = new Magic(character, Substitute.For<IItemService>());
        var requirements = new[] { 1, 1 };

        magic.RemoveRunes([RuneType.Air, RuneType.Earth], requirements);

        Assert.AreEqual(1, inventory.GetCountById((int)RuneType.Earth));
        Assert.AreEqual(1, inventory.TakenSlots);
        CollectionAssert.AreEqual(new[] { 1, 1 }, requirements);
    }

    [TestMethod]
    public void CheckRunes_WithoutWeapon_UsesOriginalRuneRequirements()
    {
        var inventory = new ItemContainer(StorageType.Normal, 4);
        inventory.Add(new RuneItem((int)RuneType.Air, 1));
        var character = CreateCharacter(inventory, weapon: null);
        var magic = new Magic(character, Substitute.For<IItemService>());
        var requirements = new[] { 1 };

        Assert.IsTrue(magic.CheckRunes([RuneType.Air], requirements));
        CollectionAssert.AreEqual(new[] { 1 }, requirements);
    }

    private static ICharacter CreateCharacter(ItemContainer inventory, IItem? weapon)
    {
        var inventoryContainer = Substitute.For<IInventoryContainer>();
        inventoryContainer.Items.Returns(inventory);
        var equipment = Substitute.For<IEquipmentContainer>();
        equipment[EquipmentSlot.Weapon].Returns(weapon);
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventoryContainer);
        character.Equipment.Returns(equipment);
        return character;
    }

    private sealed class RuneItem(int id, int count) : IItem
    {
        public int Id { get; } = id;
        public long[] ExtraData => [];
        public int Count { get; set; } = count;
        public string Name => $"Rune {Id}";
        public IItemDefinition ItemDefinition => Substitute.For<IItemDefinition>();
        public IEquipmentDefinition EquipmentDefinition => Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript => Substitute.For<IItemScript>();
        public IEquipmentScript EquipmentScript => Substitute.For<IEquipmentScript>();
        public IItem Clone() => new RuneItem(Id, Count);
        public IItem Clone(int newCount) => new RuneItem(Id, newCount);
        public bool Equals(IItem otherItem, bool ignoreCount = true) => otherItem is not null && Id == otherItem.Id && (ignoreCount || Count == otherItem.Count);
        public string? SerializeExtraData() => null;
    }
}
