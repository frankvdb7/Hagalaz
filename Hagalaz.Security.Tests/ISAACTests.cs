using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Security.Tests
{
    [TestClass]
    public class ISAACTests
    {
        [TestMethod]
        public void SameSeed_ShouldProduceSameSequence()
        {
            // Arrange
            var seed = new uint[] { 1, 2, 3, 4 };
            var isaac1 = new ISAAC(seed);
            var isaac2 = new ISAAC(seed);

            // Act & Assert
            for (int i = 0; i < 1000; i++)
            {
                Assert.AreEqual(isaac1.ReadKey(), isaac2.ReadKey(), $"Values differ at iteration {i}");
            }
        }

        [TestMethod]
        public void DifferentSeeds_ShouldProduceDifferentSequences()
        {
            // Arrange
            var seed1 = new uint[] { 1, 2, 3, 4 };
            var seed2 = new uint[] { 5, 6, 7, 8 };
            var isaac1 = new ISAAC(seed1);
            var isaac2 = new ISAAC(seed2);
            bool sequencesAreDifferent = false;

            // Act
            for (int i = 0; i < 1000; i++)
            {
                if (isaac1.ReadKey() != isaac2.ReadKey())
                {
                    sequencesAreDifferent = true;
                    break;
                }
            }

            // Assert
            Assert.IsTrue(sequencesAreDifferent, "Sequences for different seeds were identical.");
        }

        [TestMethod]
        public void ConsecutiveReads_ShouldProduceDifferentKeys()
        {
            // Arrange
            var seed = new uint[] { 1, 2, 3, 4 };
            var isaac = new ISAAC(seed);
            var key1 = isaac.ReadKey();

            // Act
            var key2 = isaac.ReadKey();

            // Assert
            Assert.AreNotEqual(key1, key2, "Consecutive calls to ReadKey returned the same value.");
        }

        [TestMethod]
        public void PeekKey_ShouldNotAdvanceState()
        {
            // Arrange
            var seed = new uint[] { 123, 456, 789, 101112 };
            var isaac = new ISAAC(seed);

            // Act
            var peekedKey = isaac.PeekKey();
            var readKey = isaac.ReadKey();

            // Assert
            Assert.AreEqual(peekedKey, readKey, "PeekKey returned a different value than the subsequent ReadKey.");
        }

        [TestMethod]
        public void PeekKey_MultiplePeeks_ShouldReturnSameKey()
        {
            // Arrange
            var seed = new uint[] { 99, 88, 77, 66 };
            var isaac = new ISAAC(seed);

            // Act
            var peekedKey1 = isaac.PeekKey();
            var peekedKey2 = isaac.PeekKey();
            var readKey = isaac.ReadKey();

            // Assert
            Assert.AreEqual(peekedKey1, peekedKey2, "Multiple calls to PeekKey returned different values.");
            Assert.AreEqual(peekedKey1, readKey, "PeekKey value did not match the subsequent ReadKey value after multiple peeks.");
        }
    }
}