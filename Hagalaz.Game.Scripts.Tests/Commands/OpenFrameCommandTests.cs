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
    public class OpenFrameCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_OpensFrame()
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
            widgetOptionalMock.AsFrame().Returns(widgetOptionalMock);
            ((IWidgetBuild)widgetOptionalMock).Build().Returns(widgetMock);

            var widgetContainerMock = Substitute.For<IWidgetContainer>();
            widgetContainerMock.CloseAll().Returns(true);

            var characterMock = Substitute.For<ICharacter>();
            characterMock.Widgets.Returns(widgetContainerMock);

            var command = new OpenFrameCommand(widgetBuilderMock);
            var args = new GameCommandArgs(characterMock, new[] { "openframe", "123" });

            // Act
            await command.Execute(args);

            // Assert
            widgetContainerMock.Received(1).OpenFrame(widgetMock);
        }

        [TestMethod]
        public async Task Execute_WhenWidgetsCannotClose_DoesNotBuildReplacementFrame()
        {
            var widgetBuilder = Substitute.For<IWidgetBuilder>();
            var widgetContainer = Substitute.For<IWidgetContainer>();
            widgetContainer.CloseAll().Returns(false);
            var character = Substitute.For<ICharacter>();
            character.Widgets.Returns(widgetContainer);
            var command = new OpenFrameCommand(widgetBuilder);

            await command.Execute(new GameCommandArgs(character, new[] { "openframe", "123" }));

            widgetBuilder.DidNotReceive().Create();
            widgetContainer.DidNotReceiveWithAnyArgs().OpenFrame(default!);
        }
    }
}
