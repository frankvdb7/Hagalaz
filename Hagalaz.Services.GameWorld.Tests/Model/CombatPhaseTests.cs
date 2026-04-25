using System;
using System.Collections.Generic;
using Hagalaz.Services.GameWorld.Model.Creatures.Combat.Experimental.Combat;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.GameWorld.Tests.Model
{
    [TestClass]
    public class CombatPhaseTests
    {
        public interface ITestCombatRotation : ICombatRotation { }

        [TestMethod]
        public void ActiveRotation_Set_HandlesNullAndEqualityCorrectly()
        {
            // Arrange
            var phase = new CombatPhase<ITestCombatRotation>();
            var rotation1 = Substitute.For<ITestCombatRotation>();
            var rotation2 = Substitute.For<ITestCombatRotation>();

            // Act & Assert
            // Initial is null
            Assert.IsNull(phase.ActiveRotation);

            // Set first rotation
            phase.ActiveRotation = rotation1;
            Assert.AreEqual(rotation1, phase.ActiveRotation);
            rotation1.Received(1).Activate();

            // Set same rotation again
            phase.ActiveRotation = rotation1;
            rotation1.Received(1).Activate(); // Should not be called again

            // Set new rotation
            phase.ActiveRotation = rotation2;
            Assert.AreEqual(rotation2, phase.ActiveRotation);
            rotation1.Received(1).Deactivate();
            rotation2.Received(1).Activate();

            // Set back to null
            phase.ActiveRotation = null!;
            Assert.IsNull(phase.ActiveRotation);
            rotation2.Received(1).Deactivate();
        }
    }
}
