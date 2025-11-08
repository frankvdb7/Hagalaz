using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Graphic;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class TileGfxCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_QueuesGraphic()
        {
            // Arrange
            var graphicBuilderMock = Substitute.For<IGraphicBuilder>();
            var graphicIdMock = Substitute.For<IGraphicId>();
            var graphicOptionalMock = Substitute.For<IGraphicOptional>();
            var graphicBuildMock = Substitute.For<IGraphicBuild>();
            var graphicMock = Substitute.For<IGraphic>();

            graphicBuilderMock.Create().Returns(graphicIdMock);
            graphicIdMock.WithId(Arg.Any<int>()).Returns(graphicOptionalMock);
            graphicOptionalMock.WithHeight(Arg.Any<int>()).Returns(graphicOptionalMock);
            graphicOptionalMock.WithRotation(Arg.Any<int>()).Returns(graphicOptionalMock);
            ((IGraphicBuild)graphicOptionalMock).Build().Returns(graphicMock);

            var characterMock = Substitute.For<ICharacter>();
            var command = new TileGfxCommand(graphicBuilderMock);
            var args = new GameCommandArgs(characterMock, new[] { "tilegfx", "1", "2", "3" });

            // Act
            await command.Execute(args);

            // Assert
            characterMock.Received(1).QueueGraphic(graphicMock);
        }
    }
}
