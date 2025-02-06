using System.Collections;
using System.Text.Json.Nodes;

namespace Hagalaz.Text.Json.Tests
{
    [TestClass]
    public class JsonFlattenerTests
    {
        [TestMethod]
        public void FlattenJson_EmptyString_ShouldReturnEmptyDictionary()
        {
            // Arrange
            const string emptyString = "";

            // Act
            var result = JsonFlattener.FlattenJson(emptyString);

            // Assert
            CollectionAssert.AreEquivalent(new Dictionary<string, JsonNode>(), (ICollection)result);
        }

        [TestMethod]
        public void FlattenJson_EmptyJson_ShouldReturnEmptyDictionary()
        {
            // Arrange
            const string emptyJson = "{}";

            // Act
            var result = JsonFlattener.FlattenJson(emptyJson);

            // Assert
            CollectionAssert.AreEquivalent(new Dictionary<string, JsonNode>(), (ICollection)result);
        }

        [TestMethod]
        public void FlattenJson_NestedJson_ShouldFlattenCorrectly()
        {
            // Arrange
            const string json = """
                                {
                                    "a":{
                                        "b":1,
                                        "c":{
                                            "d":2
                                        }
                                    },
                                    "e":[3,4],
                                    "f":5
                                }
                                """;
            var expected = new Dictionary<string, JsonNode>
            {
                { "a", JsonNode.Parse("""{ "b":1, "c":{ "d":2 }}""") },
                { "a.b", JsonValue.Create(1) },
                { "a.c.d", JsonValue.Create(2) },
                { "e", new JsonArray(JsonValue.Create(3), JsonValue.Create(4)) },
                { "f", JsonValue.Create(5) }
            };

            // Act
            var result = JsonFlattener.FlattenJson(json);

            // Assert
            Assert.AreEqual(expected["a"].ToJsonString(), result["a"].ToJsonString());
            Assert.AreEqual(expected["a.b"].ToJsonString(), result["a.b"].ToJsonString());
            Assert.AreEqual(expected["a.c.d"].ToJsonString(), result["a.c.d"].ToJsonString());
            Assert.AreEqual(expected["e"].ToJsonString(), result["e"].ToJsonString());
            Assert.AreEqual(expected["f"].ToJsonString(), result["f"].ToJsonString());
        }

        [TestMethod]
        public void UnflattenJson_EmptyDictionary_ShouldReturnEmptyJsonObject()
        {
            // Arrange
            var emptyDictionary = new Dictionary<string, JsonNode>();

            // Act
            var result = JsonFlattener.UnflattenJson(emptyDictionary);

            // Assert
            Assert.AreEqual("{}", result);
        }

        [TestMethod]
        public void UnflattenJson_NestedDictionary_ShouldReturnNestedJson()
        {
            const string json = """
                                {
                                    "a":{
                                        "b":1,
                                        "c":{
                                            "d":2
                                        }
                                    },
                                    "e":[3,4],
                                    "f":5
                                }
                                """;
            // Arrange
            var dictionary = new Dictionary<string, JsonNode>
            {
                { "a", JsonNode.Parse("""{ "b":1, "c":{ "d":2 }}""") },
                { "a.b", JsonValue.Create(1) },
                { "a.c.d", JsonValue.Create(2) },
                { "e", new JsonArray(JsonValue.Create(3), JsonValue.Create(4)) },
                { "f", JsonValue.Create(5) }
            };

            // Act
            var result = JsonFlattener.UnflattenJson(dictionary);

            // Assert
            Assert.AreEqual(JsonNode.Parse(json)!.ToJsonString(), JsonNode.Parse(result)!.ToJsonString());
        }

    }
}