using Microsoft.VisualStudio.TestTools.UnitTesting;
using Hagalaz.Services.Characters.Model;

namespace Hagalaz.Services.Characters.Tests.Model
{
    [TestClass]
    public class CharactersStatisticsDtoTests
    {
        [TestMethod]
        public void OverallExperience_ShouldSumAllSkillsExactlyOnce()
        {
            // Arrange
            var dto = new CharactersStatisticsDto
            {
                DisplayName = "Test Player",
                AgilityExp = 1,
                AttackExp = 2,
                ConstitutionExp = 4,
                ConstructionExp = 8,
                CookingExp = 16,
                CraftingExp = 32,
                DefenceExp = 64, // This is the one suspected to be added twice
                DungeoneeringExp = 128,
                FarmingExp = 256,
                FiremakingExp = 512,
                FishingExp = 1024,
                FletchingExp = 2048,
                HerbloreExp = 4096,
                HunterExp = 8192,
                MagicExp = 16384,
                MiningExp = 32768,
                PrayerExp = 65536,
                RangeExp = 131072,
                RunecraftingExp = 262144,
                SlayerExp = 524288,
                SmithingExp = 1048576,
                StrengthExp = 2097152,
                SummoningExp = 4194304,
                ThievingExp = 8388608,
                WoodcuttingExp = 16777216
            };

            // Sum of 2^0 to 2^24 is 2^25 - 1 = 33554431
            // If DefenceExp (64) is added twice, it will be 33554431 + 64 = 33554495
            double expectedExperience = 1 + 2 + 4 + 8 + 16 + 32 + 64 + 128 + 256 + 512 + 1024 + 2048 + 4096 + 8192 + 16384 + 32768 + 65536 + 131072 + 262144 + 524288 + 1048576 + 2097152 + 4194304 + 8388608 + 16777216;

            // Act
            var actualExperience = dto.OverallExperience;

            // Assert
            Assert.AreEqual(expectedExperience, actualExperience, "OverallExperience should be the sum of all individual skill experiences.");
        }
    }
}
