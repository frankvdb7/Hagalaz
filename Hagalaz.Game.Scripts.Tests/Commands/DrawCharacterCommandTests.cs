using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class DrawCharacterCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_DrawsCharacter()
        {
            // Arrange
            var widgetMock = Substitute.For<IWidget>();
            var widgetsMock = Substitute.For<IWidgetContainer>();
            widgetsMock.GetOpenWidget(Arg.Any<int>()).Returns(widgetMock);
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Widgets.Returns(widgetsMock);

            var command = new DrawCharacterCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1", "2" });

            // Act
            await command.Execute(args);

            // Assert
            widgetMock.Received(1).DrawCharacterHead(2);
        }
    }
}
