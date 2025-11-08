using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class SpawnObjectCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SpawnsObject()
        {
            // Arrange
            var gameObjectBuilderMock = Substitute.For<IGameObjectBuilder>();
            var gameObjectIdMock = Substitute.For<IGameObjectId>();
            var gameObjectLocationMock = Substitute.For<IGameObjectLocation>();
            var gameObjectOptionalMock = Substitute.For<IGameObjectOptional>();
            var gameObjectMock = Substitute.For<IGameObject>();
            var regionMock = Substitute.For<IMapRegion>();

            gameObjectBuilderMock.Create().Returns(gameObjectIdMock);
            gameObjectIdMock.WithId(Arg.Any<int>()).Returns(gameObjectLocationMock);
            gameObjectLocationMock.WithLocation(Arg.Any<Location>()).Returns(gameObjectOptionalMock);
            gameObjectOptionalMock.WithShape(Arg.Any<ShapeType>()).Returns(gameObjectOptionalMock);
            gameObjectOptionalMock.WithRotation(Arg.Any<int>()).Returns(gameObjectOptionalMock);
            gameObjectOptionalMock.Build().Returns(gameObjectMock);

            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(IGameObjectBuilder)).Returns(gameObjectBuilderMock);

            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);
            characterMock.Region.Returns(regionMock);

            var command = new SpawnObjectCommand();
            var args = new GameCommandArgs(characterMock, new[] { "123", "10", "1" });

            // Act
            await command.Execute(args);

            // Assert
            gameObjectBuilderMock.Received(1).Create();
            regionMock.Received(1).Add(Arg.Any<IGameObject>());
        }
    }
}
