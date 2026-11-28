using System.Text.Json;
using Hagalaz.Cache.Abstractions.Types;
using Hagalaz.Services.Cache.Features.Types.Dtos;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class QuestDefinitionDtoTests
{
    [TestMethod]
    public void FromQuest_ExposesVerifiedMetadataAndNothingUnproven()
    {
        IQuestType quest = new StubQuest();

        var json = JsonSerializer.SerializeToElement(GetQuestDefinitionDto.FromQuest(quest), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.AreEqual(12, json.GetProperty("id").GetInt32());
        Assert.AreEqual("Cook's Assistant", json.GetProperty("name").GetString());
        Assert.AreEqual(1, json.GetProperty("questPoints").GetInt32());
        Assert.IsTrue(json.GetProperty("members").GetBoolean());
        Assert.AreEqual(32, json.GetProperty("questPointRequirement").GetInt32());
        Assert.AreEqual(100, json.GetProperty("graphicId").GetInt32());
        var varp = json.GetProperty("progressVarps")[0];
        Assert.AreEqual(1, varp.GetProperty("id").GetInt32());
        Assert.AreEqual(1, varp.GetProperty("startedThreshold").GetInt32());
        Assert.AreEqual(2, varp.GetProperty("completedThreshold").GetInt32());
        Assert.AreEqual(3, json.GetProperty("progressVarBits")[0].GetProperty("completedThreshold").GetInt32());
        Assert.AreEqual(5, json.GetProperty("prerequisiteQuestIds")[1].GetInt32());
        Assert.AreEqual(10, json.GetProperty("skillRequirements")[0].GetProperty("level").GetInt32());
        Assert.AreEqual("Done", json.GetProperty("varpRequirements")[0].GetProperty("description").GetString());
        Assert.AreEqual(5, json.GetProperty("varbitRequirements")[0].GetProperty("maximum").GetInt32());
        Assert.AreEqual("hi", json.GetProperty("params").GetProperty("1").GetString());
        Assert.AreEqual(42, json.GetProperty("params").GetProperty("2").GetInt32());

        Assert.AreEqual(15, json.EnumerateObject().Count(), "Only the verified fields are part of the response contract.");
    }

    private sealed class StubQuest : IQuestType
    {
        public int Id => 12;
        public string Name => "Cook's Assistant";
        public int QuestPoints => 1;
        public int Category => 2;
        public int Difficulty => 1;
        public bool Members => true;
        public int GraphicId => 100;
        public int QuestPointRequirement => 32;
        public IReadOnlyList<QuestProgressThreshold> ProgressVarps => [new(1, 1, 2)];
        public IReadOnlyList<QuestProgressThreshold> ProgressVarBits => [new(256, 1, 3)];
        public IReadOnlyList<int> PrerequisiteQuestIds => [4, 5];
        public IReadOnlyList<QuestSkillRequirement> SkillRequirements => [new(15, 10)];
        public IReadOnlyList<QuestVariableRequirement> VarpRequirements => [new(10, 1, 3, "Done")];
        public IReadOnlyList<QuestVariableRequirement> VarbitRequirements => [new(256, 0, 5, "Key")];
        public IReadOnlyDictionary<int, object> ExtraData => new Dictionary<int, object> { [1] = "hi", [2] = 42 };
    }
}
