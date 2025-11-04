using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class InfinityCommandTests
    {
        [TestMethod]
        public async Task Execute_QueuesRsTickTask()
        {
            // Arrange
            var characterMock = Substitute.For<ICharacter>();
            var command = new InfinityCommand();
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert
            characterMock.Received(1).QueueTask(Arg.Any<RsTickTask>());
        }
    }
}
