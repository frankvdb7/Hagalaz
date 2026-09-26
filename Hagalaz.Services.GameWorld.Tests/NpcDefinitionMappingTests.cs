using AutoMapper;
using Hagalaz.Cache.Types;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Services.GameWorld.Data.Model;
using Hagalaz.Services.GameWorld.Profiles;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Tests;

[TestClass]
public sealed class NpcDefinitionMappingTests
{
    [TestMethod]
    public void Cache_npc_type_maps_to_concrete_npc_definition()
    {
        var configuration = new MapperConfiguration(
            expression => expression.AddProfile<NpcProfile>(),
            LoggerFactory.Create(_ => { }));
        configuration.AssertConfigurationIsValid();

        var definition = configuration.CreateMapper().Map<INpcDefinition>(new NpcType(1));

        Assert.IsInstanceOfType<NpcDefinition>(definition);
        Assert.AreEqual(1, definition.Id);
        Assert.AreEqual("null", definition.DisplayName);
    }
}
