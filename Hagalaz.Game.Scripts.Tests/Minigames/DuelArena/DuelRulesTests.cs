using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Minigames.DuelArena;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Minigames.DuelArena;

[TestClass]
public sealed class DuelRulesTests
{
    [TestMethod]
    public void ApplyRules_AllowedSlotDoesNotRemoveActiveProhibitedSlotHandler()
    {
        var character = Substitute.For<ICharacter>();
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);
        var rules = new DuelRules(_ => { });
        rules.ToggleRule(DuelRules.Rule.NoHelm);
        var combatScript = new DuelArenaCombatScript(
            contextAccessor,
            Substitute.For<ICharacter>(),
            rules,
            null,
            null,
            Substitute.For<IHintIconBuilder>());

        EventHappened<EquipAllowEvent>? equipHandler = null;
        var registeredHandler = (EventHappened)(_ => false);
        character.GetScript<DuelArenaCombatScript>().Returns(combatScript);
        character.RegisterEventHandler<EquipAllowEvent>(
                Arg.Do<EventHappened<EquipAllowEvent>>(handler => equipHandler = handler))
            .Returns(registeredHandler);

        rules.ApplyRules(character, combatScript);

        Assert.IsNotNull(equipHandler);
        var handler = equipHandler!;
        Assert.IsFalse(handler(new EquipAllowEvent(character, CreateItem(EquipmentSlot.Cape))));
        character.DidNotReceive().UnregisterEventHandler<EquipAllowEvent>(Arg.Any<EventHappened>());

        Assert.IsTrue(handler(new EquipAllowEvent(character, CreateItem(EquipmentSlot.Hat))));
        character.DidNotReceive().UnregisterEventHandler<EquipAllowEvent>(Arg.Any<EventHappened>());

        character.GetScript<DuelArenaCombatScript>().Returns((DuelArenaCombatScript?)null);
        Assert.IsFalse(handler(new EquipAllowEvent(character, CreateItem(EquipmentSlot.Cape))));
        character.Received(1).UnregisterEventHandler<EquipAllowEvent>(registeredHandler);
    }

    private static IItem CreateItem(EquipmentSlot slot)
    {
        var equipmentDefinition = Substitute.For<IEquipmentDefinition>();
        equipmentDefinition.Slot.Returns(slot);
        var item = Substitute.For<IItem>();
        item.EquipmentDefinition.Returns(equipmentDefinition);
        return item;
    }
}
