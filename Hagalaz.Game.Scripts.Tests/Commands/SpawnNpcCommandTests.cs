using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class SpawnNpcCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SpawnsNpc()
        {
            // Arrange
            var npcBuilderMock = Substitute.For<INpcBuilder>();
            var npcBuildMock = Substitute.For<INpcBuild>();
            npcBuilderMock.Create().WithId(Arg.Any<int>()).WithLocation(Arg.Any<Location>()).Returns(npcBuildMock);

            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(INpcBuilder)).Returns(npcBuilderMock);

            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);

            var command = new SpawnNpcCommand();
            var args = new GameCommandArgs(characterMock, new[] { "1" });

            // Act
            await command.Execute(args);

            // Assert
            npcBuilderMock.Received(1).Create();
            npcBuildMock.Received(1).Spawn();
        }
    }
}
