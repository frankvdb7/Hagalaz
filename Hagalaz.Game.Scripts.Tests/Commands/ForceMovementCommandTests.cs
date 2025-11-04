using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class ForceMovementCommandTests
    {
        [TestMethod]
        public async Task Execute_QueuesForceMovement()
        {
            // Arrange
            var movementBuilderMock = Substitute.For<IMovementBuilder>();
            var movementMock = Substitute.For<IMovement>();
            movementBuilderMock.Create().WithStart(Arg.Any<Location>()).WithEnd(Arg.Any<Location>()).WithEndSpeed(Arg.Any<int>()).Build().Returns(movementMock);
            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(IMovementBuilder)).Returns(movementBuilderMock);

            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);

            var command = new ForceMovementCommand();
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert
            characterMock.Received(1).QueueForceMovement(movementMock);
        }
    }
}
