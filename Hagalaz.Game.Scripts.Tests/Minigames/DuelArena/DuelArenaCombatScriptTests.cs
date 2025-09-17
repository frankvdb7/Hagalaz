using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Minigames.DuelArena;
using NSubstitute;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using AwesomeAssertions;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Scripts.Minigames.DuelArena.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Scripts.Tests.Minigames.DuelArena
{
    [TestClass]
    public class DuelArenaCombatScriptTests
    {
        [TestMethod]
        public void OnKilledBy_WithInvalidResource_DoesNotThrowException()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var target = Substitute.For<ICharacter>();
            var rules = new DuelRules(_ => { });
            var hintIconBuilder = Substitute.For<IHintIconBuilder>();

            var characterContext = Substitute.For<ICharacterContext>();
            characterContext.Character.Returns(character);

            var contextAccessor = Substitute.For<ICharacterContextAccessor>();
            contextAccessor.Context.Returns(characterContext);

            var script = new DuelArenaCombatScript(contextAccessor, target, rules, null, null, hintIconBuilder);

            // Act
            Action act = () => script.OnKilledBy(target);

            // Assert
            act.Should().NotThrow<ArgumentNullException>();
        }

        [TestMethod]
        public void Tick_WhenPlayerIsDefeated_ShouldAwardStakedItemsOnlyOnce()
        {
            // Arrange
            var victor = Substitute.For<ICharacter>();
            var loser = Substitute.For<ICharacter>();

            var victorContextAccessor = Substitute.For<ICharacterContextAccessor>();
            var victorContext = Substitute.For<ICharacterContext>();
            victorContext.Character.Returns(victor);
            victorContextAccessor.Context.Returns(victorContext);

            var loserContextAccessor = Substitute.For<ICharacterContextAccessor>();
            var loserContext = Substitute.For<ICharacterContext>();
            loserContext.Character.Returns(loser);
            loserContextAccessor.Context.Returns(loserContext);

            var rules = new DuelRules(_ => { });
            var hintIconBuilder = Substitute.For<IHintIconBuilder>();
            var hintIconType = Substitute.For<IHintIconType>();
            var hintIconEntityOptional = Substitute.For<IHintIconEntityOptional>();
            var hintIcon = Substitute.For<IHintIcon>();

            hintIconBuilder.Create().Returns(hintIconType);
            hintIconType.AtEntity(Arg.Any<IEntity>()).Returns(hintIconEntityOptional);
            hintIconEntityOptional.Build().Returns(hintIcon);

            var victorStake = new DuelContainer();
            var loserStake = new DuelContainer();
            var stakedItem = Substitute.For<IItem>();
            stakedItem.Id.Returns(4151); // Abyssal whip
            stakedItem.Count = 1;
            var itemDef = Substitute.For<IItemDefinition>();
            itemDef.TradeValue.Returns(1_000_000);
            stakedItem.ItemDefinition.Returns(itemDef);
            loserStake.Add(stakedItem);


            var victorScript = new DuelArenaCombatScript(victorContextAccessor, loser, rules, victorStake, loserStake, hintIconBuilder);
            var loserScript = new DuelArenaCombatScript(loserContextAccessor, victor, rules, loserStake, victorStake, hintIconBuilder);

            victor.GetScript<DuelArenaCombatScript>().Returns(victorScript);
            loser.GetScript<DuelArenaCombatScript>().Returns(loserScript);

            victor.IsDestroyed.Returns(false);
            loser.IsDestroyed.Returns(true);

            var victorInventory = Substitute.For<IInventoryContainer>();
            victor.Inventory.Returns(victorInventory);

            var victorMoneyPouch = Substitute.For<IMoneyPouchContainer>();
            victor.MoneyPouch.Returns(victorMoneyPouch);

            var serviceProvider = Substitute.For<IServiceProvider>();
            var duelEndScreenScript = Substitute.For<DuelEndScreenScript>(Substitute.For<ICharacterContextAccessor>());
            serviceProvider.GetService(typeof(DuelEndScreenScript)).Returns(duelEndScreenScript);
            serviceProvider.GetRequiredService(typeof(DuelEndScreenScript)).Returns(duelEndScreenScript);
            victor.ServiceProvider.Returns(serviceProvider);
            loser.ServiceProvider.Returns(serviceProvider);

            var widgets = Substitute.For<IWidgetContainer>();
            var widget = Substitute.For<IWidget>();
            widgets.GetOpenWidget(1365).Returns(widget);
            victor.Widgets.Returns(widgets);
            loser.Widgets.Returns(widgets);

            victorScript.OnRegistered();
            loserScript.OnRegistered();

            // Act
            victorScript.Tick();
            victorScript.Tick();
            victorScript.Tick();

            // Assert
            victor.Inventory.Received(1).AddRange(victorStake);
            victor.Inventory.Received(1).AddRange(loserStake);
        }

        [TestMethod]
        public void Tick_WhenPlayerIsDefeated_InNonStakingDuel_ShouldNotRespawnMultipleTimes()
        {
            // Arrange
            var victor = Substitute.For<ICharacter>();
            var loser = Substitute.For<ICharacter>();

            var victorContextAccessor = Substitute.For<ICharacterContextAccessor>();
            var victorContext = Substitute.For<ICharacterContext>();
            victorContext.Character.Returns(victor);
            victorContextAccessor.Context.Returns(victorContext);

            var loserContextAccessor = Substitute.For<ICharacterContextAccessor>();
            var loserContext = Substitute.For<ICharacterContext>();
            loserContext.Character.Returns(loser);
            loserContextAccessor.Context.Returns(loserContext);

            var rules = new DuelRules(_ => { });
            var hintIconBuilder = Substitute.For<IHintIconBuilder>();
            var hintIconType = Substitute.For<IHintIconType>();
            var hintIconEntityOptional = Substitute.For<IHintIconEntityOptional>();
            var hintIcon = Substitute.For<IHintIcon>();

            hintIconBuilder.Create().Returns(hintIconType);
            hintIconType.AtEntity(Arg.Any<IEntity>()).Returns(hintIconEntityOptional);
            hintIconEntityOptional.Build().Returns(hintIcon);

            var victorScript = new DuelArenaCombatScript(victorContextAccessor, loser, rules, null, null, hintIconBuilder);
            var loserScript = new DuelArenaCombatScript(loserContextAccessor, victor, rules, null, null, hintIconBuilder);

            victor.GetScript<DuelArenaCombatScript>().Returns(victorScript);
            loser.GetScript<DuelArenaCombatScript>().Returns(loserScript);

            victor.IsDestroyed.Returns(false);
            loser.IsDestroyed.Returns(true);

            var serviceProvider = Substitute.For<IServiceProvider>();
            var duelEndScreenScript = Substitute.For<DuelEndScreenScript>(Substitute.For<ICharacterContextAccessor>());
            serviceProvider.GetService(typeof(DuelEndScreenScript)).Returns(duelEndScreenScript);
            serviceProvider.GetRequiredService(typeof(DuelEndScreenScript)).Returns(duelEndScreenScript);
            victor.ServiceProvider.Returns(serviceProvider);
            loser.ServiceProvider.Returns(serviceProvider);

            var widgets = Substitute.For<IWidgetContainer>();
            var widget = Substitute.For<IWidget>();
            widgets.GetOpenWidget(1365).Returns(widget);
            victor.Widgets.Returns(widgets);
            loser.Widgets.Returns(widgets);

            victorScript.OnRegistered();
            loserScript.OnRegistered();

            // Act
            victorScript.Tick();
            victorScript.Tick();
            victorScript.Tick();

            // Assert
            victor.Received(1).Respawn();
            loser.Received(1).Respawn();
        }
    }
}
