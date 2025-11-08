using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Animation;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class GameObjectAnimCommandTests
    {
        [TestMethod]
        public async Task Execute_AnimatesVisibleObject()
        {
            // Arrange
            var animationBuilderMock = Substitute.For<IAnimationBuilder>();
            var animationIdMock = Substitute.For<IAnimationId>();
            var animationOptionalMock = Substitute.For<IAnimationOptional>();
            var animationBuildMock = Substitute.For<IAnimationBuild>();
            var animationMock = Substitute.For<IAnimation>();

            animationBuilderMock.Create().Returns(animationIdMock);
            animationIdMock.WithId(123).Returns(animationOptionalMock);
            ((IAnimationBuild)animationOptionalMock).Build().Returns(animationMock);

            var gameObjectMock = Substitute.For<IGameObject>();
            gameObjectMock.Id.Returns(456);

            var gameObjectServiceMock = Substitute.For<IGameObjectService>();
            gameObjectServiceMock.FindByLocation(Arg.Any<Location>()).Returns(new List<IGameObject> { gameObjectMock });

            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(IGameObjectService)).Returns(gameObjectServiceMock);

            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);
            characterMock.Location.Returns(new Location(0,0,0,0));

            var command = new GameObjectAnimCommand(animationBuilderMock);
            var args = new GameCommandArgs(characterMock, new[] { "objanim", "456", "123" });

            // Act
            await command.Execute(args);

            // Assert
            gameObjectServiceMock.Received(1).AnimateGameObject(gameObjectMock, animationMock);
        }
    }
}
