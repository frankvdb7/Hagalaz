using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Game.Abstractions.Tests.Model.Creatures.Characters
{
    [TestClass]
    public class StatisticsHelpersTests
    {
        [DataTestMethod]
        [DataRow((byte)1, 0.0)]
        [DataRow((byte)2, 83.0)]
        [DataRow((byte)99, 13034431.0)]
        [DataRow((byte)10, 1154.0)]
        [DataRow((byte)50, 101333.0)]
        public void ExperienceForLevel_CalculatesCorrectly(byte level, double expectedExperience)
        {
            double result = StatisticsHelpers.ExperienceForLevel(level);
            Assert.AreEqual(expectedExperience, result, 1e-9);
        }

        [DataTestMethod]
        [DataRow((byte)1, 0.0, (byte)1)]
        [DataRow((byte)1, 82.0, (byte)1)]
        [DataRow((byte)1, 83.0, (byte)2)]
        [DataRow((byte)2, 83.0, (byte)2)]
        [DataRow((byte)98, 13034430.0, (byte)98)]
        [DataRow((byte)98, 13034431.0, (byte)99)]
        [DataRow((byte)99, 13034431.0, (byte)99)]
        public void LevelForExperience_CalculatesCorrectly(byte startLevel, double experience, byte expectedLevel)
        {
            byte result = StatisticsHelpers.LevelForExperience(startLevel, experience);
            Assert.AreEqual(expectedLevel, result);
        }
    }
}
