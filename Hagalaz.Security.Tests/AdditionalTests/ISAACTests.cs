using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Security.Tests.AdditionalTests
{
    [TestClass]
    public class ISAACTests
    {
        [TestMethod]
        public void ISAAC_Output_ShouldMatchExpected()
        {
            // This is a characterization test. The expected values are the actual
            // output of the current implementation. This test is designed to
            // prevent unintended changes to the ISAAC cipher's behavior.
            var seed = new int[] { 1, 2, 3, 4 };
            var isaac = new ISAAC(seed);

            var expected = new int[]
            {
                -1845427519, 101887342, -587788481, 1007932596, 75525414, 189192449, 1445791764,
                -151032338, -1320498263, 1008933220, -1771143821, -1963952930, 2038531773, 1342600216,
                -1759369324, -1328017551
            };

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], isaac.NextKey());
            }
        }
    }
}
