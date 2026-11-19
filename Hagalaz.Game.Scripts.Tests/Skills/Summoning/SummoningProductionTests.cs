using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Summoning;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static Hagalaz.Game.Scripts.Tests.SkillTaskTestSupport;

namespace Hagalaz.Game.Scripts.Tests.Skills.Summoning;

[TestClass]
public sealed class SummoningProductionTests
{
    private const int Pouch = 12000;
    private const int Scroll = 12001;
    private const int Charm = 12158;
    private const int Shard = 18016;
    private const double PouchXp = 5;
    private const double ScrollXp = 2;

    private static readonly SummoningDto Definition = new()
    {
        PouchId = Pouch, ScrollId = Scroll, SummonLevel = 1, CreatePouchExperience = PouchXp, ScrollExperience = ScrollXp
    };

    [TestMethod]
    [DataRow(1)]
    [DataRow(5)]
    [DataRow(10)]
    public void TryCreatePouches_WithIngredients_ConsumesExactlyAndGrantsExperienceOnce(int count)
    {
        var (inventory, character, service) = Create();
        for (var i = 0; i < count; i++)
        {
            inventory.Items.Add(CreateItem(Charm, 1));
            inventory.Items.Add(CreateItem(Shard, 1));
        }

        Assert.IsTrue(service.TryCreatePouches(character, Definition, Pouch, count));

        Assert.AreEqual(0, inventory.Items.GetCountById(Charm));
        Assert.AreEqual(0, inventory.Items.GetCountById(Shard));
        Assert.AreEqual(count, inventory.Items.GetCountById(Pouch));
        character.Statistics.Received(1).AddExperience(Arg.Any<int>(), PouchXp * count);
    }

    [TestMethod]
    public void TryCreatePouches_WithInsufficientIngredient_ChangesNothing()
    {
        var (inventory, character, service) = Create();
        inventory.Items.Add(CreateItem(Charm, 1));
        inventory.Items.Add(CreateItem(Charm, 1));
        inventory.Items.Add(CreateItem(Shard, 1));

        Assert.IsFalse(service.TryCreatePouches(character, Definition, Pouch, 2));

        Assert.AreEqual(2, inventory.Items.GetCountById(Charm));
        Assert.AreEqual(1, inventory.Items.GetCountById(Shard));
        Assert.AreEqual(0, inventory.Items.GetCountById(Pouch));
        character.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MaxValue)]
    public void TryCreatePouches_WithInvalidAmount_ChangesNothing(int count)
    {
        var (inventory, character, service) = Create();
        inventory.Items.Add(CreateItem(Charm, 1));
        inventory.Items.Add(CreateItem(Shard, 1));

        Assert.IsFalse(service.TryCreatePouches(character, Definition, Pouch, count));

        Assert.AreEqual(1, inventory.Items.GetCountById(Charm));
        Assert.AreEqual(1, inventory.Items.GetCountById(Shard));
        character.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void TryCreateScrolls_WhenScrollsCannotBeInserted_KeepsPouchesAndGrantsNoExperience()
    {
        var (inventory, character, service) = Create();
        inventory.Items.Add(CreateItem(Pouch, 1));
        for (var i = 0; i < 27; i++) inventory.Items.Add(CreateItem(999, 1));

        Assert.IsFalse(service.TryCreateScrolls(character, Definition, 1));

        Assert.AreEqual(1, inventory.Items.GetCountById(Pouch));
        Assert.AreEqual(0, inventory.Items.GetCountById(Scroll));
        character.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void TryCreateScrolls_WithRoom_ConvertsPouchAndGrantsExperienceOnce()
    {
        var (inventory, character, service) = Create();
        inventory.Items.Add(CreateItem(Pouch, 1));

        Assert.IsTrue(service.TryCreateScrolls(character, Definition, 1));

        Assert.AreEqual(0, inventory.Items.GetCountById(Pouch));
        Assert.AreEqual(10, inventory.Items.GetCountById(Scroll));
        character.Statistics.Received(1).AddExperience(Arg.Any<int>(), ScrollXp);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-5)]
    public void TryCreateScrolls_WithInvalidAmount_ChangesNothing(int count)
    {
        var (inventory, character, service) = Create();
        inventory.Items.Add(CreateItem(Pouch, 1));

        Assert.IsFalse(service.TryCreateScrolls(character, Definition, count));

        Assert.AreEqual(1, inventory.Items.GetCountById(Pouch));
        character.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    private static (ComposedTestContainer Inventory, ICharacter Character, SummoningSkillService Service) Create()
    {
        var (inventory, character, builder, _) = CreateScenario();
        var definition = Substitute.For<IItemDefinition>();
        definition.ExtraData.Returns(new Dictionary<int, object> { [538] = Charm, [539] = 1, [540] = Shard, [541] = 1 });
        var items = Substitute.For<IItemService>();
        items.FindItemDefinitionById(Arg.Any<int>()).Returns(definition);
        character.ServiceProvider.Returns(new ServiceCollection().AddSingleton(items).BuildServiceProvider());
        character.Statistics.GetSkillLevel(Arg.Any<int>()).Returns(99);
        return (inventory, character,
            new SummoningSkillService(Substitute.For<INpcBuilder>(), Substitute.For<IFamiliarScriptProvider>(), builder));
    }
}
