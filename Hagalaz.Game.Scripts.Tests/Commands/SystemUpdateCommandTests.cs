using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class SystemUpdateCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SchedulesUpdate()
        {
            // Arrange
            var systemUpdateServiceMock = Substitute.For<ISystemUpdateService>();
            var characterMock = Substitute.For<ICharacter>();

            var command = new SystemUpdateCommand(systemUpdateServiceMock);
            var args = new GameCommandArgs(characterMock, new[] { "update", "100" });

            // Act
            await command.Execute(args);

            // Assert
            systemUpdateServiceMock.Received(1).ScheduleUpdate(100);
        }
    }
}
