using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Items.Auras;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Items;

[TestClass]
public sealed class CosmeticAuraOperationsTests
{
    [TestMethod]
    public void ActivatedAuraToggle_AddsSixteenAndPreservesCountAndSlot()
    {
        var aura = Substitute.For<IItem>();
        aura.Id.Returns(23880);
        aura.Count.Returns(7);
        var equipment = Substitute.For<IEquipmentContainer>();
        equipment.GetInstanceSlot(aura).Returns(EquipmentSlot.Aura);
        equipment.TryReplaceEquippedItem(Arg.Any<EquipmentSlot>(), aura, Arg.Any<IItem>()).Returns(true);
        var character = CreateCharacter(equipment);
        var script = new CosmeticAuraActivated();

        script.ToggleAura(character, aura);

        equipment.Received(1).TryReplaceEquippedItem(EquipmentSlot.Aura, aura,
            Arg.Is<IItem>(item => item.Id == 23896 && item.Count == 7));
    }

    [TestMethod]
    public void DeactivatedAuraToggle_SubtractsSixteenAndPreservesCountAndSlot()
    {
        var aura = Substitute.For<IItem>();
        aura.Id.Returns(23896);
        aura.Count.Returns(3);
        var equipment = Substitute.For<IEquipmentContainer>();
        equipment.GetInstanceSlot(aura).Returns(EquipmentSlot.Aura);
        equipment.TryReplaceEquippedItem(Arg.Any<EquipmentSlot>(), aura, Arg.Any<IItem>()).Returns(true);
        var character = CreateCharacter(equipment);
        var script = new CosmeticAuraDeactivated();

        script.ToggleAura(character, aura);

        equipment.Received(1).TryReplaceEquippedItem(EquipmentSlot.Aura, aura,
            Arg.Is<IItem>(item => item.Id == 23880 && item.Count == 3));
    }

    [TestMethod]
    public void ToggleAura_WhenExactAuraIsNotEquipped_DoesNotReplaceAnySlot()
    {
        var aura = Substitute.For<IItem>();
        aura.Id.Returns(23880);
        var equipment = Substitute.For<IEquipmentContainer>();
        equipment.GetInstanceSlot(aura).Returns(EquipmentSlot.NoSlot);
        var character = CreateCharacter(equipment);

        new CosmeticAuraActivated().ToggleAura(character, aura);

        equipment.DidNotReceive().TryReplaceEquippedItem(Arg.Any<EquipmentSlot>(), Arg.Any<IItem>(), Arg.Any<IItem>());
    }

    [TestMethod]
    public void ActivatedAuraOption4_UsesAuraUnequipCallback()
    {
        var aura = Substitute.For<IItem>();
        var equipmentScript = Substitute.For<IEquipmentScript>();
        aura.EquipmentScript.Returns(equipmentScript);
        var character = Substitute.For<ICharacter>();

        new CosmeticAuraActivated().ItemClickedInEquipment(ComponentClickType.Option4Click, aura, character);

        equipmentScript.Received(1).UnEquipItem(aura, character);
    }

    private static ICharacter CreateCharacter(IEquipmentContainer equipment)
    {
        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IItemBuilder)).Returns(new TestItemBuilder(ComposedTestContainer.CreateTestItem));
        var character = Substitute.For<ICharacter>();
        character.Equipment.Returns(equipment);
        character.ServiceProvider.Returns(provider);
        return character;
    }
}
