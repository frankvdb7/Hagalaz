using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Text;

namespace Hagalaz.Security.Tests
{
    [TestClass]
    public class WhirlpoolMoreTests
    {
        [TestMethod]
        public void Whirlpool_GenerateDigest_WithOffset_ShouldReturnCorrectHash()
        {
            // Arrange
            var input = "The quick brown fox jumps over the lazy dog";
            var expectedHash = "B97DE512E91E3828B40D2B0FDCE9CEB3C4A71F9BEA8D88E75C4FA854DF36725FD2B52EB6544EDCACD6F8BEDDFEA403CB55AE31F03AD62A5EF54E42EE82C3FB35";
            var data = Encoding.UTF8.GetBytes("---" + input + "---");

            // Act
            var result = Whirlpool.GenerateDigest(data, 3, input.Length);
            var actualHash = Convert.ToHexString(result);

            // Assert
            Assert.AreEqual(expectedHash, actualHash, true);
        }

        [TestMethod]
        public void Whirlpool_HashCore_And_HashFinal_ShouldReturnCorrectHash()
        {
            // Arrange
            var input = "The quick brown fox jumps over the lazy dog";
            var expectedHash = "B97DE512E91E3828B40D2B0FDCE9CEB3C4A71F9BEA8D88E75C4FA854DF36725FD2B52EB6544EDCACD6F8BEDDFEA403CB55AE31F03AD62A5EF54E42EE82C3FB35";
            var data = Encoding.UTF8.GetBytes(input);

            // Act
            using (var whirlpool = new Whirlpool())
            {
                whirlpool.Initialize();
                whirlpool.TransformBlock(data, 0, data.Length, null, 0);
                whirlpool.TransformFinalBlock(new byte[0], 0, 0);
                var result = whirlpool.Hash;
                var actualHash = Convert.ToHexString(result);

                // Assert
                Assert.AreEqual(expectedHash, actualHash, true);
            }
        }

        [TestMethod]
        public void Whirlpool_MultipleUpdates_ShouldReturnCorrectHash()
        {
            // Arrange
            var input1 = "The quick brown fox";
            var input2 = " jumps over the lazy dog";
            var expectedHash = "B97DE512E91E3828B40D2B0FDCE9CEB3C4A71F9BEA8D88E75C4FA854DF36725FD2B52EB6544EDCACD6F8BEDDFEA403CB55AE31F03AD62A5EF54E42EE82C3FB35";
            var data1 = Encoding.UTF8.GetBytes(input1);
            var data2 = Encoding.UTF8.GetBytes(input2);

            // Act
            using (var whirlpool = new Whirlpool())
            {
                whirlpool.Initialize();
                whirlpool.TransformBlock(data1, 0, data1.Length, null, 0);
                whirlpool.TransformBlock(data2, 0, data2.Length, null, 0);
                whirlpool.TransformFinalBlock(new byte[0], 0, 0);
                var result = whirlpool.Hash;
                var actualHash = Convert.ToHexString(result);

                // Assert
                Assert.AreEqual(expectedHash, actualHash, true);
            }
        }
    }
}