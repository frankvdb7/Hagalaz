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
        public async Task Execute_WithValidArguments_SendsProjectile()
        {
            // Arrange
            var projectileBuilderMock = Substitute.For<IProjectileBuilder>();
            var projectileIdMock = Substitute.For<IProjectileId>();
            var projectileFromMock = Substitute.For<IProjectileFrom>();
            var projectileToMock = Substitute.For<IProjectileTo>();
            var projectileDurationMock = Substitute.For<IProjectileDuration>();
            var projectileOptionalMock = Substitute.For<IProjectileOptional>();
            var projectileBuildMock = Substitute.For<IProjectileBuild>();

            projectileBuilderMock.Create().Returns(projectileIdMock);
            projectileIdMock.WithGraphicId(Arg.Any<int>()).Returns(projectileFromMock);
            projectileFromMock.FromCreature(Arg.Any<ICreature>()).Returns(projectileToMock);
            projectileToMock.ToCreature(Arg.Any<ICreature>()).Returns(projectileDurationMock);
            projectileDurationMock.WithDuration(Arg.Any<int>()).Returns(projectileOptionalMock);
            projectileOptionalMock.WithFromHeight(Arg.Any<int>()).Returns(projectileOptionalMock);
            projectileOptionalMock.WithToHeight(Arg.Any<int>()).Returns(projectileOptionalMock);
            projectileOptionalMock.WithSlope(Arg.Any<int>()).Returns(projectileOptionalMock);
            projectileOptionalMock.WithDelay(Arg.Any<int>()).Returns(projectileOptionalMock);
            ((IProjectileBuild)projectileOptionalMock).Send();

            var serviceProviderMock = Substitute.For<IServiceProvider>();
            serviceProviderMock.GetService(typeof(IProjectileBuilder)).Returns(projectileBuilderMock);

            var targetCreature = Substitute.For<ICreature>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);
            characterMock.Viewport.VisibleCreatures.Returns(new List<ICreature> { targetCreature });

            var command = new ProjCommand();
            var args = new GameCommandArgs(characterMock, new[] { "proj", "123" });

            // Act
            await command.Execute(args);

            // Assert
            ((IProjectileBuild)projectileOptionalMock).Received(1).Send();
        }
    }
}
