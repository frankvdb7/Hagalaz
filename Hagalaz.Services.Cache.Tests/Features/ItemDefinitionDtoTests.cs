using System.Text.Json;
using System.Text.Json.Nodes;
using Hagalaz.Cache.Types;
using Hagalaz.Services.Cache.Features;
using Hagalaz.Services.Cache.Features.Types.Dtos;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hagalaz.Services.Cache.Tests.Features;

[TestClass]
public class ItemDefinitionDtoTests
{
    [TestMethod]
    public void FullDefinitionDto_RoundTripsEveryStoredFieldAndPreservesResponseShape()
    {
        var source = new ItemType(20139);
        // Non-default values detect fields omitted from either explicit mapping.
        foreach (var property in typeof(ItemType).GetProperties().Where(property => property.SetMethod?.IsPublic == true))
        {
            object value = property.PropertyType == typeof(int) ? 37
                : property.PropertyType == typeof(sbyte) ? (sbyte)2
                : property.PropertyType == typeof(bool) ? true
                : property.PropertyType == typeof(string) ? "DTO fixture"
                : property.PropertyType == typeof(int[]) ? new[] { 7, 11 }
                : property.PropertyType == typeof(sbyte[]) ? new sbyte[] { 2, 3 }
                : property.PropertyType == typeof(string[]) ? new[] { "take", "drop", null, null, null }
                : new Dictionary<int, object> { [1000001] = 42, [1000002] = "text" };
            property.SetValue(source, value);
        }

        var dto = GetItemDefinitionDto.FromItem(source);
        var sourceJson = JsonSerializer.SerializeToNode(source);
        Assert.IsTrue(JsonNode.DeepEquals(sourceJson, JsonSerializer.SerializeToNode(dto)), "DTO response must retain every existing JSON field.");
        var submitted = JsonSerializer.Deserialize<UpdateItemDefinitionDto>(JsonSerializer.Serialize(dto));
        Assert.IsNotNull(submitted);
        var requestJson = JsonSerializer.SerializeToNode(submitted)!.AsObject();
        foreach (var derivedField in new[] { "Noted", "Stackable", "HasWearModel", "DegradeType", "RenderAnimationId", "HasDestroyOption" })
        {
            Assert.IsFalse(requestJson.ContainsKey(derivedField), $"{derivedField} belongs only to the read contract.");
        }
        var target = new ItemType(source.Id);
        submitted.ApplyTo(target);
        Assert.IsTrue(JsonNode.DeepEquals(sourceJson, JsonSerializer.SerializeToNode(target)), "Both mappings must retain every stored field, including JSON extra data.");
    }
}
