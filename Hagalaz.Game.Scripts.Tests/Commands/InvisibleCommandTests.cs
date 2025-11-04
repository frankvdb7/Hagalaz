using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class InvisibleCommandTests
    {
        [TestMethod]
        public async Task Execute_DoesNotThrowException()
        {
            // Arrange
            var characterMock = Substitute.For<ICharacter>();
            var command = new InvisibleCommand();
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert - no exception thrown
        }
    }
}
