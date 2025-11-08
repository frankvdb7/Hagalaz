using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class OpenShopCommandTests
    {
        [TestMethod]
        public async Task Execute_WithValidArguments_SendsOpenShopEvent()
        {
            // Arrange
            var eventManagerMock = Substitute.For<Hagalaz.Game.Abstractions.Data.IEventManager>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.EventManager.Returns(eventManagerMock);

            var command = new OpenShopCommand();
            var args = new GameCommandArgs(characterMock, new[] { "123" });

            // Act
            await command.Execute(args);

            // Assert
            eventManagerMock.Received(1).SendEvent(Arg.Is<Hagalaz.Game.Common.Events.Character.OpenShopEvent>(e => e.ShopId == 123));
        }
    }
}
