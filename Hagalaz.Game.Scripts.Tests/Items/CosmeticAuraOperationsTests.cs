using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Dialogues.Generic;
using Hagalaz.Game.Scripts.Equipment.Auras;
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

    [TestMethod]
    public void InactiveEquippedAura_DelegatesEquipImmediately()
    {
        var (activeAura, incomingAura, character, equipment, widgets, _, equipmentScript) = CreateActiveAura();
        var inactiveAuraScript = Substitute.For<IItemScript>();
        activeAura.ItemScript.Returns(inactiveAuraScript);
        equipment.EquipItem(incomingAura).Returns(true);

        Assert.IsTrue(equipmentScript.EquipItem(incomingAura, character));

        equipment.Received(1).EquipItem(incomingAura);
        widgets.DidNotReceive().OpenDialogue(Arg.Any<IDialogueScript>(), Arg.Any<bool>());
    }

    [TestMethod]
    public void ActiveAuraReplacement_CancellationClosesDialogueWithoutEquipping()
    {
        var (activeAura, incomingAura, character, equipment, widgets, dialogue, equipmentScript) = CreateActiveAura();

        Assert.IsFalse(equipmentScript.EquipItem(incomingAura, character));
        widgets.Received(1).OpenDialogue(dialogue, true);

        dialogue.OnOpen();
        dialogue.OnDialogueOptionClick("No.", -1, -1);
        dialogue.OnDialogueOptionClick("Yes.", -1, -1);

        Assert.AreSame(activeAura, equipment[EquipmentSlot.Aura]);
        equipment.DidNotReceive().EquipItem(Arg.Any<IItem>());
        widgets.Received(1).CloseChatboxOverlay();
    }

    [TestMethod]
    public void ActiveAuraReplacement_YesUsesEquipmentScriptEntryPointOnce()
    {
        var (_, incomingAura, character, equipment, widgets, dialogue, equipmentScript) = CreateActiveAura();

        Assert.IsFalse(equipmentScript.EquipItem(incomingAura, character));
        dialogue.OnOpen();
        dialogue.OnDialogueOptionClick("Yes.", -1, -1);
        dialogue.OnDialogueOptionClick("Yes.", -1, -1);

        equipment.Received(1).EquipItem(incomingAura);
        widgets.Received(1).CloseChatboxOverlay();
    }

    [TestMethod]
    public void ActiveAuraReplacement_StaleConfirmationDoesNotEquipIncomingItem()
    {
        var (activeAura, incomingAura, character, equipment, _, dialogue, equipmentScript) = CreateActiveAura();
        IItem currentlyEquipped = activeAura;
        equipment[EquipmentSlot.Aura].Returns(_ => currentlyEquipped);

        Assert.IsFalse(equipmentScript.EquipItem(incomingAura, character));
        currentlyEquipped = Substitute.For<IItem>();
        dialogue.OnOpen();
        dialogue.OnDialogueOptionClick("Yes.", -1, -1);

        equipment.DidNotReceive().EquipItem(Arg.Any<IItem>());
    }

    private static (IItem ActiveAura, IItem IncomingAura, ICharacter Character, IEquipmentContainer Equipment,
        IWidgetContainer Widgets, YesNoDialogueScript Dialogue, TestAuraEquipmentScript EquipmentScript) CreateActiveAura()
    {
        var activeAura = Substitute.For<IItem>();
        activeAura.ItemScript.Returns(new TestAuraItemScript());
        activeAura.ExtraData.Returns([1, DateTime.UtcNow.AddMinutes(5).Ticks]);
        var incomingAura = Substitute.For<IItem>();
        incomingAura.EquipmentDefinition.Slot.Returns(EquipmentSlot.Aura);
        var serviceProvider = Substitute.For<IServiceProvider>();
        var character = Substitute.For<ICharacter>();
        var equipment = Substitute.For<IEquipmentContainer>();
        equipment[EquipmentSlot.Aura].Returns(activeAura);
        var widgets = Substitute.For<IWidgetContainer>();
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);
        var dialogue = new YesNoDialogueScript(contextAccessor);
        dialogue.Initialize(Substitute.For<IWidget>());
        serviceProvider.GetService(typeof(YesNoDialogueScript)).Returns(dialogue);
        character.ServiceProvider.Returns(serviceProvider);
        character.Equipment.Returns(equipment);
        character.Widgets.Returns(widgets);
        character.Configurations.Returns(Substitute.For<IConfigurations>());
        widgets.When(container => container.CloseChatboxOverlay()).Do(_ => dialogue.OnClose());
        return (activeAura, incomingAura, character, equipment, widgets, dialogue, new TestAuraEquipmentScript());
    }

    private sealed class TestAuraEquipmentScript : AuraEquipmentScript { }

    private sealed class TestAuraItemScript : AuraItemScript
    {
        public override int GetTier(ICharacter character, IItem aura) => 0;
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
