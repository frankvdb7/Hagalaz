using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class SearchNpcCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SearchesNpcs()
        {
            // Arrange
            var npcServiceMock = Substitute.For<INpcService>();
            var npcDefinitionMock = Substitute.For<INpcDefinition>();
            npcDefinitionMock.Name.Returns("Test Npc");
            npcServiceMock.GetNpcDefinitionCount().Returns(1);
            npcServiceMock.FindNpcDefinitionById(0).Returns(npcDefinitionMock);

            var characterMock = Substitute.For<ICharacter>();
            var command = new SearchNpcCommand(npcServiceMock);
            var args = new GameCommandArgs(characterMock, new[] { "test" });

            // Act
            await command.Execute(args);

            // Assert
            characterMock.Received(1).SendChatMessage(Arg.Is<string>(s => s.Contains("Test Npc")), messageType: ChatMessageType.ConsoleText);
        }
    }
}
