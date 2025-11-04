using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class GenericGameCommandTests
    {
        [TestMethod]
        public async Task Execute_CallsAction()
        {
            // Arrange
            var characterMock = Substitute.For<ICharacter>();
            var actionMock = Substitute.For<Func<ICharacter, string[], Task>>();

            var command = new GenericGameCommand("test", Abstractions.Authorization.Permission.Standard, actionMock);
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert
            await actionMock.Received(1).Invoke(characterMock, []);
        }
    }
}
