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
                DefenceExp = 10, // Only once here
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
    }
}
