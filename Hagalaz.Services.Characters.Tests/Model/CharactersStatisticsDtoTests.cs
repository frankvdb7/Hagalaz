using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Services.Characters.Model;

namespace Hagalaz.Services.Characters.Tests.Model
{
    [TestClass]
    public class CharactersStatisticsDtoTests
    {
        [TestMethod]
        public void OverallExperience_ShouldSumAllExperienceCorrectly()
        {
            // Arrange
            var stats = new CharactersStatisticsDto
            {
                DisplayName = "TestPlayer",
                AgilityExp = 10,
                AttackExp = 10,
                ConstitutionExp = 10,
                ConstructionExp = 10,
                CookingExp = 10,
                CraftingExp = 10,
                DefenceExp = 10,
                DungeoneeringExp = 10,
                FarmingExp = 10,
                FiremakingExp = 10,
                FishingExp = 10,
                FletchingExp = 10,
                HerbloreExp = 10,
                HunterExp = 10,
                MagicExp = 10,
                MiningExp = 10,
                PrayerExp = 10,
                RangeExp = 10,
                RunecraftingExp = 10,
                SlayerExp = 10,
                SmithingExp = 10,
                StrengthExp = 10,
                SummoningExp = 10,
                ThievingExp = 10,
                WoodcuttingExp = 10
            };

            // There are 25 skills in total.
            // 25 * 10 = 250
            double expected = 250;

            // Act
            double actual = stats.OverallExperience;

            // Assert
            Assert.AreEqual(expected, actual, "OverallExperience calculation is incorrect. It might be double-counting a skill (e.g., DefenceExp).");
        }

        [TestMethod]
        public void OverallLevel_ShouldSumAllLevelsCorrectly()
        {
            // Arrange
            var stats = new CharactersStatisticsDto
            {
                DisplayName = "TestPlayer",
                AgilityLevel = 1,
                AttackLevel = 1,
                ConstitutionLevel = 1,
                ConstructionLevel = 1,
                CookingLevel = 1,
                CraftingLevel = 1,
                DefenceLevel = 1,
                DungeoneeringLevel = 1,
                FarmingLevel = 1,
                FiremakingLevel = 1,
                FishingLevel = 1,
                FletchingLevel = 1,
                HerbloreLevel = 1,
                HunterLevel = 1,
                MagicLevel = 1,
                MiningLevel = 1,
                PrayerLevel = 1,
                RangeLevel = 1,
                RunecraftingLevel = 1,
                SlayerLevel = 1,
                SmithingLevel = 1,
                StrengthLevel = 1,
                SummoningLevel = 1,
                ThievingLevel = 1,
                WoodcuttingLevel = 1
            };

            // There are 25 skills in total.
            // 25 * 1 = 25
            int expected = 25;

            // Act
            int actual = stats.OverallLevel;

            // Assert
            Assert.AreEqual(expected, actual, "OverallLevel calculation is incorrect.");
        }
    }
}
