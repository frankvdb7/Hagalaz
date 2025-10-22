using Xunit;

namespace Hagalaz.Security.Tests.AdditionalTests
{
    public class ISAACTests
    {
        [Fact]
        public void ISAAC_Output_ShouldMatchExpected()
        {
            // This is a characterization test. The expected values are the actual
            // output of the current implementation. This test is designed to
            // prevent unintended changes to the ISAAC cipher's behavior.
            var seed = new uint[] { 1, 2, 3, 4 };
            var isaac = new ISAAC(seed);

            var expected = new uint[]
            {
                3673720382,
                1957022519,
                2949967219,
                2273082436,
                2412264859,
                1616913581,
                4286187434,
                1337573575,
                3564981768,
                3931377724,
                180676801,
                4234301014,
                3123903540,
                804531392,
                1687941800,
                3180870208
            };

            for (int i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], isaac.ReadKey());
            }
        }
    }
}
