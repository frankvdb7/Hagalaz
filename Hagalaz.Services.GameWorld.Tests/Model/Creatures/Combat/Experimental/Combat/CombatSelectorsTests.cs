using Hagalaz.Services.GameWorld.Model.Creatures.Combat.Experimental.Combat;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace Hagalaz.Services.GameWorld.Tests.Model.Creatures.Combat.Experimental.Combat
{
    [TestClass]
    public class CombatSelectorsTests
    {
        [TestMethod]
        public void ProbabilitySelector_WithValidRotations_ReturnsRotation()
        {
            // Arrange
            var rotation1 = Substitute.For<ICombatRotation>();
            rotation1.Probability.Returns(1.0);
            var rotations = new List<ICombatRotation> { rotation1 };
            var selector = CombatSelectors.ProbabilitySelector<ICombatRotation>();

            // Act
            var result = selector(rotations);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(rotation1, result);
        }

        [TestMethod]
        public void ProbabilitySelector_WithSmallProbabilities_ReturnsRotation()
        {
            // Arrange
            var rotation1 = Substitute.For<ICombatRotation>();
            rotation1.Probability.Returns(0.1);
            var rotations = new List<ICombatRotation> { rotation1 };
            var selector = CombatSelectors.ProbabilitySelector<ICombatRotation>();

            // Act
            var result = selector(rotations);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(rotation1, result);
        }

        [TestMethod]
        public void ProbabilitySelector_WithEmptyList_ThrowsInvalidOperationException()
        {
            // Arrange
            var rotations = new List<ICombatRotation>();
            var selector = CombatSelectors.ProbabilitySelector<ICombatRotation>();

            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() => selector(rotations));
        }
    }
}
