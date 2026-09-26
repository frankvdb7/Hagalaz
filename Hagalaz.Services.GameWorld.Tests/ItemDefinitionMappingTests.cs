using AutoMapper;
using Hagalaz.Cache.Types;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Profiles;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class ItemDefinitionMappingTests
{
    [TestMethod]
    public void Cache_item_type_maps_to_concrete_item_definition()
    {
        var configuration = new MapperConfiguration(
            expression => expression.AddProfile<ItemProfile>(),
            LoggerFactory.Create(_ => { }));
        configuration.AssertConfigurationIsValid();

        var definition = configuration.CreateMapper().Map<IItemDefinition>(new ItemType(995));

        Assert.IsInstanceOfType<ItemDefinition>(definition);
        Assert.AreEqual(995, definition.Id);
    }
}
