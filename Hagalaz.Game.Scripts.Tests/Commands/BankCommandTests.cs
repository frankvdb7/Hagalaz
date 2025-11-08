using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class BankCommandTests
    {
        [TestMethod]
        public async Task Execute_DoesNotThrowException()
        {
            // Arrange
            var serviceProviderMock = Substitute.For<IServiceProvider>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.ServiceProvider.Returns(serviceProviderMock);

            var bankScreen = Substitute.For<Hagalaz.Game.Scripts.Widgets.Bank.BankScreen>(Substitute.For<Hagalaz.Game.Abstractions.Providers.ICharacterContextAccessor>(), Substitute.For<Hagalaz.Game.Abstractions.Mediator.IScopedGameMediator>());
            serviceProviderMock.GetService(typeof(Hagalaz.Game.Scripts.Widgets.Bank.BankScreen)).Returns(bankScreen);

            var command = new BankCommand();
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert - The BankCommand is currently empty, so we just ensure no exceptions are thrown.
        }
    }
}
