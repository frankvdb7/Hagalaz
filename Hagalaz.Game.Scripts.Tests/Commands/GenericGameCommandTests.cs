using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class GenericGameCommandTests
    {
        [TestMethod]
        public async Task Execute_CallsCommandFunc()
        {
            // Arrange
            var characterMock = Substitute.For<ICharacter>();
            var commandFuncMock = Substitute.For<Func<ICharacter, string[], Task<bool>>>();

            var command = new GenericGameCommand
            {
                Name = "test",
                Permission = Hagalaz.Game.Abstractions.Authorization.Permission.Standard,
                CommandFunc = commandFuncMock
            };
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert
            await commandFuncMock.Received(1).Invoke(characterMock, []);
        }
    }
}
