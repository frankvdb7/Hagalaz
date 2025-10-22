using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Game.Abstractions.Tests.Model.Creatures.Characters
{
    [TestClass]
    public class StatisticsHelpersTests
    {
        [DataTestMethod]
        [DataRow(0, 0.0)]
        [DataRow(1, 0.0)]
        [DataRow(2, 83.0)]
        [DataRow(99, 13034431.0)]
        [DataRow(10, 1154.0)]
        [DataRow(50, 101333.0)]
        public void ExperienceForLevel_CalculatesCorrectly(int level, double expectedExperience)
        {
            double result = StatisticsHelpers.ExperienceForLevel(level);
            Assert.AreEqual(expectedExperience, result, 1e-9);
        }

        [DataTestMethod]
        [DataRow((byte)StatisticsConstants.Attack, 0.0, (byte)1)]
        [DataRow((byte)StatisticsConstants.Attack, 82.0, (byte)1)]
        [DataRow((byte)StatisticsConstants.Attack, 83.0, (byte)2)]
        [DataRow((byte)StatisticsConstants.Attack, 13034430.0, (byte)98)]
        [DataRow((byte)StatisticsConstants.Attack, 13034431.0, (byte)99)]
        [DataRow((byte)StatisticsConstants.Attack, 14000000.0, (byte)99)] // Exceeds max
        public void LevelForExperience_CalculatesCorrectly(byte skillId, double experience, byte expectedLevel)
        {
            byte result = StatisticsHelpers.LevelForExperience(skillId, experience);
            Assert.AreEqual(expectedLevel, result);
        }

        [DataTestMethod]
        [DataRow(10000000.0, (byte)96)]
        [DataRow(13034431.0, (byte)99)]
        [DataRow(104273167.0, (byte)120)]
        [DataRow(111945110.0, (byte)120)]
        [DataRow(200000000.0, (byte)120)] // Exceeds max
        public void LevelForExperience_DungeoneeringCalculatesCorrectly(double experience, byte expectedLevel)
        {
            byte result = StatisticsHelpers.LevelForExperience(StatisticsConstants.Dungeoneering, experience);
            Assert.AreEqual(expectedLevel, result);
        }

        [TestMethod]
        public void GetLifePointsNormalizeRate_ReturnsCorrectValue()
        {
            Assert.AreEqual(10, StatisticsHelpers.GetLifePointsNormalizeRate());
        }

        [TestMethod]
        public void GetLifePointsRestoreRate_ReturnsCorrectValue()
        {
            Assert.AreEqual(10, StatisticsHelpers.GetLifePointsRestoreRate());
        }

        [TestMethod]
        public void GetPoisonRate_ReturnsCorrectValue()
        {
            Assert.AreEqual(30, StatisticsHelpers.GetPoisonRate());
        }

        [TestMethod]
        public void GetRunEnergyDrainRate_ReturnsCorrectValue()
        {
            Assert.AreEqual(400, StatisticsHelpers.GetRunEnergyDrainRate());
        }

        [TestMethod]
        public void GetSpecialEnergyRestoreRate_ReturnsCorrectValue()
        {
            Assert.AreEqual(50, StatisticsHelpers.GetSpecialEnergyRestoreRate());
        }

        [TestMethod]
        public void GetStatisticsNormalizeRate_ReturnsCorrectValue()
        {
            Assert.AreEqual(100, StatisticsHelpers.GetStatisticsNormalizeRate());
        }
    }
}
