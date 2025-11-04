using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class SaveNpcsCommandTests
    {
        [TestMethod]
        public async Task Execute_SendsDisabledMessage()
        {
            // Arrange
            var characterMock = Substitute.For<ICharacter>();
            var command = new SaveNpcsCommand();
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert
            characterMock.Received(1).SendChatMessage("This command is currently disabled.");
        }
    }
}
