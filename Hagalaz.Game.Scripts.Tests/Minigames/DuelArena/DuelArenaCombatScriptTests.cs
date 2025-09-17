using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Minigames.DuelArena;
using NSubstitute;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Builders.HintIcon;
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
    }
}
