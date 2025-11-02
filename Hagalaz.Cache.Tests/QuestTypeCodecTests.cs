using Hagalaz.Cache.Types;
using Xunit;
using Hagalaz.Cache.Logic.Codecs;

namespace Hagalaz.Cache.Tests;

public class QuestTypeCodecTests
{
    [Fact]
    public void EncodeDecode_RoundTrip_ShouldPreserveData()
    {
        // Arrange
        var codec = new QuestTypeCodec();
        var originalQuest = new QuestType(1)
        {
            Name = "Test Quest",
            QuestPoints = 1,
            Category = 2,
            Difficulty = 3,
            Members = true,
            QuestPointRequirement = 10,
            GraphicId = 100,
            SortName = "A Test Quest",
            ProgressVarps = new[,] { { 1, 2, 3 }, { 4, 5, 6 } },
            ProgressVarBits = new[,] { { 7, 8, 9 }, { 10, 11, 12 } },
            QuestRequirements = new[] { 1, 2, 3 },
            StatRequirements = new[,] { { 1, 10 }, { 2, 20 } },
            VarpRequirements = new[] { 100, 101 },
            MinVarpValue = new[] { 1, 2 },
            MaxVarpValue = new[] { 10, 20 },
            VarpRequirementNames = new[] { "Varp1", "Varp2" },
            VarBitRequirements = new[] { 200, 201 },
            MinVarBitValue = new[] { 3, 4 },
            MaxVarBitValue = new[] { 30, 40 },
            VarbitRequirementNames = new[] { "Varbit1", "Varbit2" },
            ExtraData = new Dictionary<int, object>
            {
                { 1, "Test" },
                { 2, 123 }
            }
        };

        // Act
        var encodedStream = codec.Encode(originalQuest);
        encodedStream.Position = 0;
        var decodedQuest = (QuestType)codec.Decode(originalQuest.Id, encodedStream);

        // Assert
        Assert.Equal(originalQuest.Id, decodedQuest.Id);
        Assert.Equal(originalQuest.Name, decodedQuest.Name);
        Assert.Equal(originalQuest.QuestPoints, decodedQuest.QuestPoints);
        Assert.Equal(originalQuest.Category, decodedQuest.Category);
        Assert.Equal(originalQuest.Difficulty, decodedQuest.Difficulty);
        Assert.Equal(originalQuest.Members, decodedQuest.Members);
        Assert.Equal(originalQuest.QuestPointRequirement, decodedQuest.QuestPointRequirement);
        Assert.Equal(originalQuest.GraphicId, decodedQuest.GraphicId);
        Assert.Equal(originalQuest.SortName, decodedQuest.SortName);
        Assert.True(originalQuest.ProgressVarps.Cast<int>().SequenceEqual(decodedQuest.ProgressVarps.Cast<int>()));
        Assert.True(originalQuest.ProgressVarBits.Cast<int>().SequenceEqual(decodedQuest.ProgressVarBits.Cast<int>()));
        Assert.True(originalQuest.QuestRequirements.SequenceEqual(decodedQuest.QuestRequirements));
        Assert.True(originalQuest.StatRequirements.Cast<int>().SequenceEqual(decodedQuest.StatRequirements.Cast<int>()));
        Assert.True(originalQuest.VarpRequirements.SequenceEqual(decodedQuest.VarpRequirements));
        Assert.True(originalQuest.MinVarpValue.SequenceEqual(decodedQuest.MinVarpValue));
        Assert.True(originalQuest.MaxVarpValue.SequenceEqual(decodedQuest.MaxVarpValue));
        Assert.True(originalQuest.VarpRequirementNames.SequenceEqual(decodedQuest.VarpRequirementNames));
        Assert.True(originalQuest.VarBitRequirements.SequenceEqual(decodedQuest.VarBitRequirements));
        Assert.True(originalQuest.MinVarBitValue.SequenceEqual(decodedQuest.MinVarBitValue));
        Assert.True(originalQuest.MaxVarBitValue.SequenceEqual(decodedQuest.MaxVarBitValue));
        Assert.True(originalQuest.VarbitRequirementNames.SequenceEqual(decodedQuest.VarbitRequirementNames));
        Assert.Equal(originalQuest.ExtraData.Count, decodedQuest.ExtraData.Count);
        Assert.Equal(originalQuest.ExtraData[1], decodedQuest.ExtraData[1]);
        Assert.Equal(originalQuest.ExtraData[2], decodedQuest.ExtraData[2]);
    }
}