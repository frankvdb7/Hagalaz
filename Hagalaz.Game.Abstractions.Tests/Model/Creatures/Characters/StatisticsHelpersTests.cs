using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Game.Abstractions.Tests.Model.Creatures.Characters;

[TestClass]
public class StatisticsHelpersTests
{
    [DataTestMethod]
    [DataRow((byte)1, 0.0)]
    [DataRow((byte)2, 83.0)]
    [DataRow((byte)10, 1154.0)]
    [DataRow((byte)50, 101333.0)]
    [DataRow((byte)99, 13034431.0)]
    public void ExperienceForLevel_CalculatesCorrectly(byte level, double expectedExperience)
    {
        double actualExperience = StatisticsHelpers.ExperienceForLevel(level);
        Assert.AreEqual(expectedExperience, actualExperience, 1e-9);
    }

    [DataTestMethod]
    [DataRow(0.0, (byte)1)]
    [DataRow(83.0, (byte)2)]
    [DataRow(174.0, (byte)3)]
    [DataRow(1154.0, (byte)10)]
    [DataRow(101333.0, (byte)50)]
    [DataRow(13034431.0, (byte)99)]
    [DataRow(14000000.0, (byte)99)]
    public void LevelForExperience_CalculatesCorrectly(double experience, byte expectedLevel)
    {
        byte actualLevel = StatisticsHelpers.LevelForExperience(1, experience);
        Assert.AreEqual(expectedLevel, actualLevel);
    }
}
