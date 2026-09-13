using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Minigames.DuelArena;
using NSubstitute;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Common.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using AwesomeAssertions;

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
        public void OnRegistered_TracksDestroyEventsForBothParticipants()
        {
            var character = Substitute.For<ICharacter>();
            var target = Substitute.For<ICharacter>();
            var rules = new DuelRules(_ => { });
            var hintIconBuilder = Substitute.For<IHintIconBuilder>();
            var hintIconType = Substitute.For<IHintIconType>();
            var hintIconTarget = Substitute.For<IHintIconEntityOptional>();
            var hintIcon = Substitute.For<IHintIcon>();
            hintIconBuilder.Create().Returns(hintIconType);
            hintIconType.AtEntity(target).Returns(hintIconTarget);
            hintIconTarget.Build().Returns(hintIcon);
            var destroyHandler = (EventHappened)(_ => false);
            character.RegisterEventHandler<CreatureDestroyedEvent>(Arg.Any<EventHappened<CreatureDestroyedEvent>>()).Returns(destroyHandler);
            target.RegisterEventHandler<CreatureDestroyedEvent>(Arg.Any<EventHappened<CreatureDestroyedEvent>>()).Returns(destroyHandler);

            var context = Substitute.For<ICharacterContext>();
            context.Character.Returns(character);
            var contextAccessor = Substitute.For<ICharacterContextAccessor>();
            contextAccessor.Context.Returns(context);
            var script = new DuelArenaCombatScript(contextAccessor, target, rules, null, null, hintIconBuilder);

            script.OnRegistered();
            script.OnRemove();

            character.Received(1).RegisterEventHandler<CreatureDestroyedEvent>(Arg.Any<EventHappened<CreatureDestroyedEvent>>());
            target.Received(1).RegisterEventHandler<CreatureDestroyedEvent>(Arg.Any<EventHappened<CreatureDestroyedEvent>>());
            character.Received(1).UnregisterEventHandler<CreatureDestroyedEvent>(destroyHandler);
            target.Received(1).UnregisterEventHandler<CreatureDestroyedEvent>(destroyHandler);
        }
    }
}
