using System;
using System.IO;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Cache.Logic.Codecs;
using Hagalaz.Cache.Types;
using Xunit;

namespace Hagalaz.Cache.Tests;

public class QuestTypeCodecTests
{
    /// <summary>
    /// A quest definition assembled by hand from the revision-742 client layout (not produced by the encoder), exercising
    /// every opcode the client reads. Opcodes ascend, so a faithful encoder reproduces it byte for byte.
    /// </summary>
    private static readonly byte[] RepresentativeBytes = Convert.FromHexString(string.Concat(
        "01" + "00" + "436F6F6B00",                                 // 1  name: version byte, "Cook"
        "02" + "00" + "657600",                                      // 2  unverified string: "ev"
        "03" + "01" + "0001" + "00000001" + "00000002",              // 3  progress varp   [1, started 1, completed 2]
        "04" + "01" + "0100" + "00000001" + "00000003",              // 4  progress varbit [256, started 1, completed 3]
        "05" + "0007",                                               // 5  unsigned short, discarded by the client
        "06" + "02",                                                 // 6  category
        "07" + "01",                                                 // 7  difficulty
        "08",                                                        // 8  members
        "09" + "01",                                                 // 9  quest points
        "0A" + "02" + "00050003" + "00060004",                       // 10 int array (component | interface << 16)
        "0C" + "00000100",                                           // 12 int, discarded by the client
        "0D" + "02" + "0004" + "0005",                               // 13 prerequisite quests 4, 5
        "0E" + "02" + "0F0A" + "124B",                               // 14 skill 15 level 10, skill 18 level 75
        "0F" + "0020",                                               // 15 quest point requirement 32
        "11" + "0064",                                               // 17 graphic 100
        "12" + "01" + "0000000A" + "00000001" + "00000003" + "446F6E6500", // 18 varp 10 in [1, 3] "Done"
        "13" + "01" + "00000100" + "00000000" + "00000005" + "4B657900",   // 19 varbit 256 in [0, 5] "Key"
        "F9" + "02" + "01" + "000001" + "686900" + "00" + "000002" + "0000002A", // 249 params {1:"hi", 2:42}
        "00"));

    private readonly QuestTypeCodec _codec = new();

    private IQuestType Decode(int id, byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return _codec.Decode(id, stream);
    }

    private byte[] EncodeToBytes(IQuestType quest)
    {
        using var stream = _codec.Encode(quest);
        return stream.ToArray();
    }

    [Fact]
    public void Decode_RepresentativeBytes_ExposesVerifiedMetadataThroughTheAbstraction()
    {
        IQuestType quest = Decode(3, RepresentativeBytes);

        Assert.Equal(3, quest.Id);
        Assert.Equal("Cook", quest.Name);
        Assert.Equal(2, quest.Category);
        Assert.Equal(1, quest.Difficulty);
        Assert.True(quest.Members);
        Assert.Equal(1, quest.QuestPoints);
        Assert.Equal(32, quest.QuestPointRequirement);
        Assert.Equal(100, quest.GraphicId);
        Assert.Equal([new QuestProgressThreshold(1, 1, 2)], quest.ProgressVarps);
        Assert.Equal([new QuestProgressThreshold(256, 1, 3)], quest.ProgressVarBits);
        Assert.Equal([4, 5], quest.PrerequisiteQuestIds);
        Assert.Equal([new QuestSkillRequirement(15, 10), new QuestSkillRequirement(18, 75)], quest.SkillRequirements);
        Assert.Equal([new QuestVariableRequirement(10, 1, 3, "Done")], quest.VarpRequirements);
        Assert.Equal([new QuestVariableRequirement(256, 0, 5, "Key")], quest.VarbitRequirements);
        Assert.Equal(2, quest.ExtraData.Count);
        Assert.Equal("hi", quest.ExtraData[1]);
        Assert.Equal(42, quest.ExtraData[2]);
    }

    [Fact]
    public void Decode_RepresentativeBytes_KeepsUnverifiedOpcodesUnderNeutralNames()
    {
        var quest = (QuestType)Decode(3, RepresentativeBytes);

        Assert.Equal("ev", quest.Opcode2Text);
        Assert.Equal(7, quest.Opcode5Value);
        Assert.Equal([0x00050003, 0x00060004], quest.Opcode10Values);
        Assert.Equal(0x100, quest.Opcode12Value);
    }

    [Fact]
    public void Encode_DecodedRepresentativeQuest_ReproducesTheSourceBytes()
    {
        var quest = Decode(3, RepresentativeBytes);

        Assert.Equal(RepresentativeBytes, EncodeToBytes(quest));
    }

    [Fact]
    public void Encode_AfterMutatingAVerifiedField_PreservesTheUnprovenOpcodes()
    {
        var quest = (QuestType)Decode(3, RepresentativeBytes);
        quest.Name = "Cooking";
        quest.QuestPoints = 5;

        var reloaded = (QuestType)Decode(3, EncodeToBytes(quest));

        Assert.Equal("Cooking", reloaded.Name);
        Assert.Equal(5, reloaded.QuestPoints);
        Assert.Equal("ev", reloaded.Opcode2Text);
        Assert.Equal(7, reloaded.Opcode5Value);
        Assert.Equal(quest.Opcode10Values, reloaded.Opcode10Values);
        Assert.Equal(0x100, reloaded.Opcode12Value);
    }

    [Fact]
    public void Decode_EmptyDefinition_YieldsDefaultsAndEncodesToTheTerminatorOnly()
    {
        var quest = (QuestType)Decode(9, [0]);

        Assert.Equal(string.Empty, quest.Name);
        Assert.Equal(-1, quest.GraphicId);
        Assert.Empty(quest.ProgressVarps);
        Assert.Empty(quest.ExtraData);
        Assert.Null(quest.Opcode2Text);
        Assert.Null(quest.Opcode5Value);
        Assert.Equal([0], EncodeToBytes(quest));
    }

    [Fact]
    public void Decode_RepeatedParamKey_KeepsTheLastValue()
    {
        var bytes = Convert.FromHexString("F9" + "02" + "00" + "000001" + "00000001" + "00" + "000001" + "00000002" + "00");

        var quest = Decode(1, bytes);

        Assert.Equal(2, Assert.Single(quest.ExtraData).Value);
    }

    [Fact]
    public void Decode_UnknownOpcode_FailsInsteadOfDesynchronisingTheStream()
    {
        var exception = Assert.Throws<InvalidDataException>(() => Decode(1, [0x0B, 0x00]));

        Assert.Contains("opcode 11", exception.Message);
    }

    [Fact]
    public void Decode_TruncatedDefinition_Throws()
    {
        Assert.ThrowsAny<IOException>(() => Decode(3, RepresentativeBytes[..^8]));
    }

    // Raw bytes of quest 84 from the local revision-742 cache. Opcode 5 precedes opcode 1, so real data is not in ascending order.
    private static readonly byte[] RealQuest84 = Convert.FromHexString(
        "090105001E010052656369706520666F722044697361737465723A205374617274696E67204F7574000401073A000000010000000200");

    // Raw bytes of quest 77 from the local revision-742 cache: opcode 2 with literal quotes, opcode 10, skills and params.
    private static readonly byte[] RealQuest77 = Convert.FromHexString(
        "09010200224D697373696E67204D79204D756D6D792028736F727429220007010801004D697373696E67204D79204D756D6D7900"
        + "040117E00000000A0000003C0A01030A4CBF0D03008D000300CD0E0506230C23052307231623"
        + "F9030000034F0000009B0100034D4D697373696E67204D79204D756D6D7900000003B80000253D00");

    [Fact]
    public void Decode_RealQuestWithOpcode5BeforeTheName_ReadsEveryField()
    {
        var quest = (QuestType)Decode(84, RealQuest84);

        Assert.Equal("Recipe for Disaster: Starting Out", quest.Name);
        Assert.Equal(1, quest.QuestPoints);
        Assert.Equal(30, quest.Opcode5Value);
        Assert.Equal([new QuestProgressThreshold(1850, 1, 2)], quest.ProgressVarBits);
    }

    [Fact]
    public void Decode_RealQuestWithSkillsOpcode10AndParams_ReadsEveryField()
    {
        var quest = (QuestType)Decode(77, RealQuest77);

        Assert.Equal("Missing My Mummy", quest.Name);
        Assert.Equal("\"Missing My Mummy (sort)\"", quest.Opcode2Text);
        Assert.Equal(1, quest.Difficulty);
        Assert.True(quest.Members);
        Assert.Equal(1, quest.QuestPoints);
        Assert.Equal([new QuestProgressThreshold(6112, 10, 60)], quest.ProgressVarBits);
        Assert.Equal([0x030A4CBF], quest.Opcode10Values);
        Assert.Equal([141, 3, 205], quest.PrerequisiteQuestIds);
        Assert.Equal([6, 12, 5, 7, 22], quest.SkillRequirements.Select(skill => skill.SkillId));
        Assert.All(quest.SkillRequirements, skill => Assert.Equal(35, skill.Level));
        Assert.Equal(155, quest.ExtraData[847]);
        Assert.Equal("Missing My Mummy", quest.ExtraData[845]);
        Assert.Equal(9533, quest.ExtraData[952]);
    }

    [Theory]
    [InlineData(84)]
    [InlineData(77)]
    public void Encode_DecodedRealQuest_DecodesBackToTheSameDefinition(int id)
    {
        var source = (QuestType)Decode(id, id == 84 ? RealQuest84 : RealQuest77);

        var reloaded = (QuestType)Decode(id, EncodeToBytes(source));

        Assert.Equal(source.Name, reloaded.Name);
        Assert.Equal(source.QuestPoints, reloaded.QuestPoints);
        Assert.Equal(source.Difficulty, reloaded.Difficulty);
        Assert.Equal(source.Members, reloaded.Members);
        Assert.Equal(source.ProgressVarBits, reloaded.ProgressVarBits);
        Assert.Equal(source.PrerequisiteQuestIds, reloaded.PrerequisiteQuestIds);
        Assert.Equal(source.SkillRequirements, reloaded.SkillRequirements);
        Assert.Equal(source.ExtraData.OrderBy(pair => pair.Key), reloaded.ExtraData.OrderBy(pair => pair.Key));
        Assert.Equal(source.Opcode2Text, reloaded.Opcode2Text);
        Assert.Equal(source.Opcode5Value, reloaded.Opcode5Value);
        Assert.Equal(source.Opcode10Values, reloaded.Opcode10Values);
    }

    [Fact]
    public void Encode_ListLongerThanOneByteCount_Throws()
    {
        var quest = new QuestType(1) { PrerequisiteQuestIds = Enumerable.Range(0, 256).ToArray() };

        Assert.Throws<InvalidOperationException>(() => _codec.Encode(quest));
    }
}
