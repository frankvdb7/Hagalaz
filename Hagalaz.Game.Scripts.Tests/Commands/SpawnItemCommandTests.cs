using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class SpawnItemCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SpawnsItem()
        {
            // Arrange
            var groundItemBuilderMock = Substitute.For<IGroundItemBuilder>();
            var groundItemBuildMock = Substitute.For<IGroundItemBuild>();
            groundItemBuilderMock.Create().WithItem(Arg.Any<Action<IItemBuilder>>()).WithLocation(Arg.Any<Location>()).WithTicks(Arg.Any<int>()).Returns(groundItemBuildMock);

            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(IGroundItemBuilder)).Returns(groundItemBuilderMock);

            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);

            var command = new SpawnItemCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1", "2", "3" });

            // Act
            await command.Execute(args);

            // Assert
            groundItemBuilderMock.Received(1).Create();
            groundItemBuildMock.Received(1).Spawn();
        }
    }
}
