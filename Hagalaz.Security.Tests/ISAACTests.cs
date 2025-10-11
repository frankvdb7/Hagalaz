using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Security.Tests
{
    [TestClass]
    public class ISAACTests
    {
        [TestMethod]
        public void Constructor_WithNullSeed_ThrowsArgumentNullException()
        {
            // Arrange
            uint[] seed = null;

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new ISAAC(seed));
        }

        [TestMethod]
        public void ReadKey_WithSameSeed_GeneratesSameKeys()
        {
            // Arrange
            var seed = new uint[] { 1, 2, 3, 4 };
            var isaac1 = new ISAAC(seed);
            var isaac2 = new ISAAC(seed);

            // Act
            var key1 = isaac1.ReadKey();
            var key2 = isaac2.ReadKey();

            // Assert
            Assert.AreEqual(key1, key2);
        }

        [TestMethod]
        public void ReadKey_WithDifferentSeeds_GeneratesDifferentKeys()
        {
            // Arrange
            var seed1 = new uint[] { 1, 2, 3, 4 };
            var seed2 = new uint[] { 5, 6, 7, 8 };
            var isaac1 = new ISAAC(seed1);
            var isaac2 = new ISAAC(seed2);

            // Act
            var key1 = isaac1.ReadKey();
            var key2 = isaac2.ReadKey();

            // Assert
            Assert.AreNotEqual(key1, key2);
        }

        [TestMethod]
        public void PeekKey_DoesNotAdvanceKey()
        {
            // Arrange
            var seed = new uint[] { 1, 2, 3, 4 };
            var isaac = new ISAAC(seed);

            // Act
            var peekedKey = isaac.PeekKey();
            var readKey = isaac.ReadKey();

            // Assert
            Assert.AreEqual(peekedKey, readKey);
        }
    }
}