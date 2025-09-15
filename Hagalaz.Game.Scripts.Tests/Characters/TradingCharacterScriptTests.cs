using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Characters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Builders.Item;
using NSubstitute;
using AwesomeAssertions;

namespace Hagalaz.Game.Scripts.Tests.Characters
{
    [TestClass]
    public class TradingCharacterScriptTests
    {
        [TestMethod]
        public void CancelTradeSession_WithNullProperties_DoesNotThrowException()
        {
            // Arrange
            var character = Substitute.For<ICharacter>();
            var widgets = Substitute.For<IWidgetContainer>();
            character.Widgets.Returns(widgets);

            var characterContext = Substitute.For<ICharacterContext>();
            characterContext.Character.Returns(character);

            var contextAccessor = Substitute.For<ICharacterContextAccessor>();
            contextAccessor.Context.Returns(characterContext);

            var itemBuilder = Substitute.For<IItemBuilder>();
            var script = new TradingCharacterScript(contextAccessor, itemBuilder);

            // Act
            Action act = () => script.CancelTradeSession();

            // Assert
            act.Should().NotThrow();
        }
    }
}
