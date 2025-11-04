using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Builders.Projectile;
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
    public class ProjCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArgumentsAndVisibleCreature_SendsProjectile()
        {
            // Arrange
            var projectileBuilderMock = Substitute.For<IProjectileBuilder>();
            var projectileCreateBuilderMock = Substitute.For<IProjectileCreateBuilder>();

            projectileBuilderMock.Create().Returns(projectileCreateBuilderMock);

            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(IProjectileBuilder)).Returns(projectileBuilderMock);

            var targetCreatureMock = Substitute.For<ICreature>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);
            characterMock.Viewport.VisibleCreatures.Returns(new List<ICreature> { characterMock, targetCreatureMock });

            var command = new ProjCommand();
            var args = new GameCommandArgs(characterMock, new[] { "123" });

            // Act
            await command.Execute(args);

            // Assert
            projectileBuilderMock.Received(1).Create();
        }

        [TestMethod]
        public async Task Execute_WithNoVisibleCreatures_DoesNotSendProjectile()
        {
            // Arrange
            var projectileBuilderMock = Substitute.For<IProjectileBuilder>();
            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(IProjectileBuilder)).Returns(projectileBuilderMock);

            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);
            characterMock.Viewport.VisibleCreatures.Returns(new List<ICreature> { characterMock });

            var command = new ProjCommand();
            var args = new GameCommandArgs(characterMock, new[] { "123" });

            // Act
            await command.Execute(args);

            // Assert
            projectileBuilderMock.DidNotReceive().Create();
        }
    }
}
