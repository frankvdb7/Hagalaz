using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Minigames.DuelArena;
using Microsoft.Extensions.Logging;
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
            Substitute.For<IHintIconBuilder>(),
            Substitute.For<ILogger<DuelArenaCombatScript>>());

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
        combatScript.OnRemove();
        Assert.IsFalse(handler(new EquipAllowEvent(character, CreateItem(EquipmentSlot.Cape))));
        character.Received(1).UnregisterEventHandler<EquipAllowEvent>(registeredHandler);
    }

    [TestMethod]
    public void ApplyRules_AllowedAttackDoesNotRemoveActiveCombatStyleRestriction()
    {
        var cases = new[]
        {
            (DuelRules.Rule.NoMelee, AttackStyle.MagicNormal, AttackStyle.MeleeAccurate, "Melee has been disabled."),
            (DuelRules.Rule.NoRanged, AttackStyle.MagicNormal, AttackStyle.RangedRapid, "Ranged has been disabled."),
            (DuelRules.Rule.NoMagic, AttackStyle.MeleeAccurate, AttackStyle.MagicNormal, "Magic has been disabled.")
        };

        foreach (var (rule, allowedStyle, prohibitedStyle, message) in cases)
        {
            var character = Substitute.For<ICharacter>();
            var context = Substitute.For<ICharacterContext>();
            context.Character.Returns(character);
            var contextAccessor = Substitute.For<ICharacterContextAccessor>();
            contextAccessor.Context.Returns(context);
            var rules = new DuelRules(_ => { });
            rules.ToggleRule(rule);
            var combatScript = new DuelArenaCombatScript(
                contextAccessor,
                Substitute.For<ICharacter>(),
                rules,
                null,
                null,
                Substitute.For<IHintIconBuilder>(),
                Substitute.For<ILogger<DuelArenaCombatScript>>());

            EventHappened<AttackAllowEvent>? attackHandler = null;
            var registeredHandler = (EventHappened)(_ => false);
            character.GetScript<DuelArenaCombatScript>().Returns(combatScript);
            character.RegisterEventHandler<AttackAllowEvent>(
                    Arg.Do<EventHappened<AttackAllowEvent>>(handler => attackHandler = handler))
                .Returns(registeredHandler);

            rules.ApplyRules(character, combatScript);

            Assert.IsNotNull(attackHandler);
            Assert.IsFalse(attackHandler!(new AttackAllowEvent(character, allowedStyle)));
            character.DidNotReceive().UnregisterEventHandler<AttackAllowEvent>(Arg.Any<EventHappened>());

            Assert.IsTrue(attackHandler(new AttackAllowEvent(character, prohibitedStyle)));
            character.Received(1).SendChatMessage(message);
            character.DidNotReceive().UnregisterEventHandler<AttackAllowEvent>(Arg.Any<EventHappened>());

            character.GetScript<DuelArenaCombatScript>().Returns((DuelArenaCombatScript?)null);
            combatScript.OnRemove();
            Assert.IsFalse(attackHandler(new AttackAllowEvent(character, allowedStyle)));
            character.Received(1).UnregisterEventHandler<AttackAllowEvent>(registeredHandler);
        }
    }

    [TestMethod]
    public void ApplyRules_UnEquipIsRejectedOnlyWhileCombatScriptIsActive()
    {
        var character = Substitute.For<ICharacter>();
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);
        var combatScript = new DuelArenaCombatScript(
            contextAccessor,
            Substitute.For<ICharacter>(),
            new DuelRules(_ => { }),
            null,
            null,
            Substitute.For<IHintIconBuilder>(),
            Substitute.For<ILogger<DuelArenaCombatScript>>());

        EventHappened<UnEquipAllowEvent>? unEquipHandler = null;
        var registeredHandler = (EventHappened)(_ => false);
        character.GetScript<DuelArenaCombatScript>().Returns(combatScript);
        character.RegisterEventHandler<UnEquipAllowEvent>(
                Arg.Do<EventHappened<UnEquipAllowEvent>>(handler => unEquipHandler = handler))
            .Returns(registeredHandler);

        combatScript.Rules.ApplyRules(character, combatScript);

        Assert.IsNotNull(unEquipHandler);
        Assert.IsTrue(unEquipHandler!(new UnEquipAllowEvent(character, Substitute.For<IItem>())));
        character.Received(1).SendChatMessage("You cannot unequip items during a duel.");

        character.GetScript<DuelArenaCombatScript>().Returns((DuelArenaCombatScript?)null);
        combatScript.OnRemove();
        Assert.IsFalse(unEquipHandler(new UnEquipAllowEvent(character, Substitute.For<IItem>())));
        character.Received(1).UnregisterEventHandler<UnEquipAllowEvent>(registeredHandler);
    }

    [TestMethod]
    public void ApplyRulesThenRemove_RepeatedDuelsUnregisterRuleHandlers()
    {
        var character = Substitute.For<ICharacter>();
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(character);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);
        var registeredEquipHandlers = new List<EventHappened>();
        var unregisteredEquipHandlers = new List<EventHappened>();
        var registeredUnequipHandlers = new List<EventHappened>();
        var unregisteredUnequipHandlers = new List<EventHappened>();

        character.RegisterEventHandler<EquipAllowEvent>(Arg.Any<EventHappened<EquipAllowEvent>>())
            .Returns(_ =>
            {
                EventHappened handler = _ => false;
                registeredEquipHandlers.Add(handler);
                return handler;
            });
        character.UnregisterEventHandler<EquipAllowEvent>(Arg.Do<EventHappened>(unregisteredEquipHandlers.Add))
            .Returns(true);
        character.RegisterEventHandler<UnEquipAllowEvent>(Arg.Any<EventHappened<UnEquipAllowEvent>>())
            .Returns(_ =>
            {
                EventHappened handler = _ => false;
                registeredUnequipHandlers.Add(handler);
                return handler;
            });
        character.UnregisterEventHandler<UnEquipAllowEvent>(Arg.Do<EventHappened>(unregisteredUnequipHandlers.Add))
            .Returns(true);

        for (var duel = 0; duel < 2; duel++)
        {
            var rules = new DuelRules(_ => { });
            rules.ToggleRule(DuelRules.Rule.NoHelm);
            rules.ToggleRule(DuelRules.Rule.EnableSummoning);
            var combatScript = new DuelArenaCombatScript(
                contextAccessor,
                Substitute.For<ICharacter>(),
                rules,
                null,
                null,
                Substitute.For<IHintIconBuilder>(),
                Substitute.For<ILogger<DuelArenaCombatScript>>());

            rules.ApplyRules(character, combatScript);
            combatScript.OnRemove();

            Assert.AreEqual(duel + 1, unregisteredEquipHandlers.Count);
            Assert.AreEqual(duel + 1, unregisteredUnequipHandlers.Count);
        }

        CollectionAssert.AreEqual(registeredEquipHandlers, unregisteredEquipHandlers);
        CollectionAssert.AreEqual(registeredUnequipHandlers, unregisteredUnequipHandlers);
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
