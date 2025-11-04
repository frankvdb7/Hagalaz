using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class GetCollisionCommandTests
    {
        [TestMethod]
        public async Task Execute_SendsCollisionMessage()
        {
            // Arrange
            var mapRegionMock = Substitute.For<IMapRegion>();
            mapRegionMock.GetCollision(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>()).Returns(123);

            var mapRegionServiceMock = Substitute.For<IMapRegionService>();
            mapRegionServiceMock.GetOrCreateMapRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<bool>()).Returns(mapRegionMock);

            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(IMapRegionService)).Returns(mapRegionServiceMock);

            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);
            characterMock.Location.Returns(new Location(1, 2, 3, 4));

            var command = new GetCollisionCommand();
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert
            characterMock.Received(1).SendChatMessage("Collision:123", ChatMessageType.ConsoleText);
        }
    }
}
