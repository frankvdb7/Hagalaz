using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
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
        public async Task Execute_WithValidArguments_QueuesForceMovement()
        {
            // Arrange
            var movementBuilderMock = Substitute.For<IMovementBuilder>();
            var movementLocationStartMock = Substitute.For<IMovementLocationStart>();
            var movementLocationEndMock = Substitute.For<IMovementLocationEnd>();
            var movementOptionalMock = Substitute.For<IMovementOptional>();
            var movementBuildMock = Substitute.For<IMovementBuild>();
            var movementMock = Substitute.For<IForceMovement>();

            movementBuilderMock.Create().Returns(movementLocationStartMock);
            movementLocationStartMock.WithStart(Arg.Any<Location>()).Returns(movementLocationEndMock);
            movementLocationEndMock.WithEnd(Arg.Any<Location>()).Returns(movementOptionalMock);
            movementOptionalMock.WithEndSpeed(Arg.Any<int>()).Returns(movementOptionalMock);
            ((IMovementBuild)movementOptionalMock).Build().Returns(movementMock);

            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(IMovementBuilder)).Returns(movementBuilderMock);

            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);
            characterMock.Location.Returns(new Location(0,0,0,0));

            var command = new ForceMovementCommand();
            var args = new GameCommandArgs(characterMock, new[] { "forcemovement" });

            // Act
            await command.Execute(args);

            // Assert
            characterMock.Received(1).QueueForceMovement(movementMock);
        }
    }
}
