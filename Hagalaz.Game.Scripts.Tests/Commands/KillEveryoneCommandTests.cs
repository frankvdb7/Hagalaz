using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Commands
{
    [TestClass]
    public class KillEveryoneCommandTests
    {
        [TestMethod]
        public async Task Execute_PerformsAttackOnVisibleNpcs()
        {
            // Arrange
            var combatMock = Substitute.For<ICreatureCombat>();
            var npcMock = Substitute.For<INpc>();
            var characterMock = Substitute.For<ICharacter>();
            characterMock.Combat.Returns(combatMock);
            characterMock.Viewport.VisibleCreatures.Returns(new List<ICreature> { npcMock });

            var command = new KillEveryoneCommand();
            var args = new GameCommandArgs(characterMock, []);

            // Act
            await command.Execute(args);

            // Assert
            combatMock.Received(1).PerformAttack(Arg.Is<AttackParams>(a => a.Target == npcMock));
        }
    }
}
