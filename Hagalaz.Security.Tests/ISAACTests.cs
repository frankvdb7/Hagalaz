using System;
using Xunit;

namespace Hagalaz.Security.Tests
{
    public class ISAACTests
    {
        [Fact]
        public void SameSeed_ShouldProduceSameSequence()
        {
            // Arrange
            var seed = new uint[] { 1, 2, 3, 4 };
            var isaac1 = new ISAAC(seed);
            var isaac2 = new ISAAC(seed);

            // Act & Assert
            for (int i = 0; i < 1000; i++)
            {
                Assert.Equal(isaac1.ReadKey(), isaac2.ReadKey());
            }
        }

        [Fact]
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
            Assert.True(sequencesAreDifferent, "Sequences for different seeds were identical.");
        }

        [Fact]
        public void ConsecutiveReads_ShouldProduceDifferentKeys()
        {
            // Arrange
            var seed = new uint[] { 1, 2, 3, 4 };
            var isaac = new ISAAC(seed);
            var key1 = isaac.ReadKey();

            // Act
            var key2 = isaac.ReadKey();

            // Assert
            Assert.NotEqual(key1, key2);
        }

        [Fact]
        public void PeekKey_ShouldNotAdvanceState()
        {
            // Arrange
            var seed = new uint[] { 123, 456, 789, 101112 };
            var isaac = new ISAAC(seed);

            // Act
            var peekedKey = isaac.PeekKey();
            var readKey = isaac.ReadKey();

            // Assert
            Assert.Equal(peekedKey, readKey);
        }

        [Fact]
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
            Assert.Equal(peekedKey1, peekedKey2);
            Assert.Equal(peekedKey1, readKey);
        }

        [Fact]
        public void CustomIsaac_WithNullSeed_ShouldProduceCorrectSequence()
        {
            // This test validates the output of the project's custom ISAAC implementation,
            // which differs from the official Bob Jenkins test vector.

            // Arrange
            var seed = new uint[256]; // All-zero seed
            var isaac = new ISAAC(seed);

            // The reference test program runs isaac() twice before printing results.
            // The first call is from randinit(). We need to run it one more time
            // to align our state with the test vector.
            for (int i = 0; i < 256; i++)
            {
                isaac.ReadKey();
            }

            var expectedValues = new uint[]
            {
                0x7a68710f, 0x6554abda, 0x90c10757, 0xb5e435f,
                0xaf7d1fb8, 0x1913fd3, 0x6a158d10, 0xb8f6fd4a,
                0xc2b9aa36, 0x96da2655, 0xfe1e42d5, 0x56e6cd21,
                0xd5b2d750, 0x7229ea81, 0x5de87abb, 0xb6b9d766
            };

            var actualValues = new uint[expectedValues.Length];

            // Act
            for (int i = 0; i < expectedValues.Length; i++)
            {
                actualValues[i] = isaac.ReadKey();
            }

            // Assert
            Assert.Equal(expectedValues, actualValues);
        }

        [Fact]
        public void EncryptDecrypt_Symmetry()
        {
            // Arrange
            var seed = new uint[] { 0xDEAD, 0xBEEF, 0xCAFE, 0xBABE };
            var originalPlaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var encrypted = new byte[originalPlaintext.Length];
            var decrypted = new byte[originalPlaintext.Length];

            var encryptIsaac = new ISAAC(seed);
            var decryptIsaac = new ISAAC(seed);

            // Act: Encrypt
            for (int i = 0; i < originalPlaintext.Length; i++)
            {
                encrypted[i] = (byte)(originalPlaintext[i] ^ (encryptIsaac.ReadKey() & 0xFF));
            }

            // Act: Decrypt
            for (int i = 0; i < encrypted.Length; i++)
            {
                decrypted[i] = (byte)(encrypted[i] ^ (decryptIsaac.ReadKey() & 0xFF));
            }

            // Assert
            Assert.Equal(originalPlaintext, decrypted);
        }

        [Fact]
        public void Constructor_WithNullSeed_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ISAAC(null!));
        }
    }
}