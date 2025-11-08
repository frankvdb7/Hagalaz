using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class OpenInterfaceCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_OpensInterface()
        {
            // Arrange
            var widgetBuilderMock = Substitute.For<IWidgetBuilder>();
            var widgetCharacterMock = Substitute.For<IWidgetCharacter>();
            var widgetIdMock = Substitute.For<IWidgetId>();
            var widgetOptionalMock = Substitute.For<IWidgetOptional>();
            var widgetBuildMock = Substitute.For<IWidgetBuild>();
            var widgetMock = Substitute.For<IWidget>();

            widgetBuilderMock.Create().Returns(widgetCharacterMock);
            widgetCharacterMock.ForCharacter(Arg.Any<ICharacter>()).Returns(widgetIdMock);
            widgetIdMock.WithId(Arg.Any<int>()).Returns(widgetOptionalMock);
            widgetOptionalMock.WithParentId(Arg.Any<int>()).Returns(widgetOptionalMock);
            widgetOptionalMock.WithParentSlot(Arg.Any<int>()).Returns(widgetOptionalMock);
            widgetOptionalMock.WithTransparency(Arg.Any<int>()).Returns(widgetOptionalMock);
            ((IWidgetBuild)widgetOptionalMock).Build().Returns(widgetMock);

            var gameClientMock = Substitute.For<IGameClient>();
            gameClientMock.IsScreenFixed.Returns(true);

            var currentFrameMock = Substitute.For<IWidget>();
            currentFrameMock.Id.Returns(1);

            var widgetContainerMock = Substitute.For<IWidgetContainer>();
            widgetContainerMock.CurrentFrame.Returns(currentFrameMock);

            var characterMock = Substitute.For<ICharacter>();
            characterMock.GameClient.Returns(gameClientMock);
            characterMock.Widgets.Returns(widgetContainerMock);

            var command = new OpenInterfaceCommand(widgetBuilderMock);
            var args = new GameCommandArgs(characterMock, new[] { "123" });

            // Act
            await command.Execute(args);

            // Assert
            widgetContainerMock.Received(1).OpenWidget(widgetMock);
        }
    }
}
