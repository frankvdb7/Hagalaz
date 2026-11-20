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
    private const int Shard = 12183;
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
    public void TryCreatePouches_WithStackedIngredientsInFullInventory_RollsBackBothStacksWhenOutputsDoNotFit()
    {
        var (inventory, character, service) = CreateStacked();
        inventory.Items.Add(new StackItem(Charm, 10));
        inventory.Items.Add(new StackItem(Shard, 10));
        for (var i = 0; i < 26; i++) inventory.Items.Add(new StackItem(900 + i, 1, stackable: false));
        Assert.AreEqual(28, inventory.Items.TakenSlots);

        // Infuse-10: both stacks are drained (2 slots free) but 10 nonstackable pouches need 10 slots.
        Assert.IsFalse(service.TryCreatePouches(character, Definition, Pouch, 10));

        Assert.AreEqual(10, inventory.Items.GetCountById(Charm));
        Assert.AreEqual(10, inventory.Items.GetCountById(Shard));
        Assert.AreEqual(0, inventory.Items.GetCountById(Pouch));
        Assert.AreEqual(28, inventory.Items.TakenSlots);
        character.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void TryCreatePouches_WithPartiallyConsumedStacksInFullInventory_RollsBackAndKeepsStackCounts()
    {
        var (inventory, character, service) = CreateStacked();
        inventory.Items.Add(new StackItem(Charm, 10));
        inventory.Items.Add(new StackItem(Shard, 10));
        for (var i = 0; i < 26; i++) inventory.Items.Add(new StackItem(900 + i, 1, stackable: false));

        // Stacks remain after removal, so no slot is freed for the 3 pouches.
        Assert.IsFalse(service.TryCreatePouches(character, Definition, Pouch, 3));

        Assert.AreEqual(10, inventory.Items.GetCountById(Charm));
        Assert.AreEqual(10, inventory.Items.GetCountById(Shard));
        Assert.AreEqual(0, inventory.Items.GetCountById(Pouch));
        character.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void TryCreatePouches_WithStackedIngredientsAndEnoughOutputSlots_ConsumesStacksAndGrantsExperience()
    {
        var (inventory, character, service) = CreateStacked();
        inventory.Items.Add(new StackItem(Charm, 10));
        inventory.Items.Add(new StackItem(Shard, 10));
        for (var i = 0; i < 16; i++) inventory.Items.Add(new StackItem(900 + i, 1, stackable: false));

        Assert.IsTrue(service.TryCreatePouches(character, Definition, Pouch, 10));

        Assert.AreEqual(0, inventory.Items.GetCountById(Charm));
        Assert.AreEqual(0, inventory.Items.GetCountById(Shard));
        Assert.AreEqual(10, inventory.Items.GetCountById(Pouch));
        character.Statistics.Received(1).AddExperience(Arg.Any<int>(), PouchXp * 10);
    }

    [TestMethod]
    public void TryCreatePouches_InfuseAll28WithStackedIngredientsAndEmptySlots_CreatesTwentyEightPouches()
    {
        var (inventory, character, service) = CreateStacked();
        inventory.Items.Add(new StackItem(Charm, 28));
        inventory.Items.Add(new StackItem(Shard, 28));

        Assert.IsTrue(service.TryCreatePouches(character, Definition, Pouch, 28));

        Assert.AreEqual(0, inventory.Items.GetCountById(Charm));
        Assert.AreEqual(0, inventory.Items.GetCountById(Shard));
        Assert.AreEqual(28, inventory.Items.GetCountById(Pouch));
        character.Statistics.Received(1).AddExperience(Arg.Any<int>(), PouchXp * 28);
    }

    [TestMethod]
    public void TryCreateScrolls_WithMorePouchesRequestedThanOwned_ConvertsOnlyOwnedPouches()
    {
        var (inventory, character, service) = Create();
        inventory.Items.Add(CreateItem(Pouch, 1));
        inventory.Items.Add(CreateItem(Pouch, 1));

        Assert.IsTrue(service.TryCreateScrolls(character, Definition, int.MaxValue));

        Assert.AreEqual(0, inventory.Items.GetCountById(Pouch));
        Assert.AreEqual(20, inventory.Items.GetCountById(Scroll));
        character.Statistics.Received(1).AddExperience(Arg.Any<int>(), ScrollXp * 2);
    }

    // Spirit wolf pouch from the 742-era reference (docs/revision-742/reference/summoning.md; wiki.darkan.org and
    // runescape.wiki Pouch_(Summoning)): empty pouch + gold charm + 7 spirit shards + wolf bones. The IDs are
    // wiki-documented; ExtraData is built from them in the cache pair layout (even key = item id, odd key = amount).
    // Cache-exported stackability is not available in the repository, so charm and empty-pouch stackability are
    // parameterised below while spirit shards are treated as stackable.
    private const int WolfPouch = 12047;
    private const int EmptyPouch = 12155;
    private const int WolfBones = 2859;
    private const int ShardsPerWolfPouch = 7;

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void WolfPouch_BatchOfFiveWithAllInputs_ConsumesExactRecipeAndGrantsExperienceOnce(bool charmStackable)
    {
        var (inventory, character, service) = CreateWolf(charmStackable, emptyPouchStackable: false);
        for (var i = 0; i < 5; i++) inventory.Items.Add(new StackItem(EmptyPouch, 1, stackable: false));
        for (var i = 0; i < 5; i++) inventory.Items.Add(new StackItem(WolfBones, 1, stackable: false));
        inventory.Items.Add(new StackItem(Charm, 7));
        inventory.Items.Add(new StackItem(Shard, 40));

        Assert.IsTrue(service.TryCreatePouches(character, Definition, WolfPouch, 5));

        Assert.AreEqual(0, inventory.Items.GetCountById(EmptyPouch));
        Assert.AreEqual(0, inventory.Items.GetCountById(WolfBones));
        Assert.AreEqual(2, inventory.Items.GetCountById(Charm));
        Assert.AreEqual(40 - 5 * ShardsPerWolfPouch, inventory.Items.GetCountById(Shard));
        Assert.AreEqual(5, inventory.Items.GetCountById(WolfPouch));
        character.Statistics.Received(1).AddExperience(Arg.Any<int>(), PouchXp * 5);
    }

    [TestMethod]
    public void WolfPouch_WithoutTertiaryBones_ChangesNothingAndGrantsNothing()
    {
        var (inventory, character, service) = CreateWolf(charmStackable: true, emptyPouchStackable: false);
        inventory.Items.Add(new StackItem(EmptyPouch, 1, stackable: false));
        inventory.Items.Add(new StackItem(Charm, 1));
        inventory.Items.Add(new StackItem(Shard, ShardsPerWolfPouch));

        Assert.IsFalse(service.TryCreatePouches(character, Definition, WolfPouch, 1));

        Assert.AreEqual(1, inventory.Items.GetCountById(EmptyPouch));
        Assert.AreEqual(1, inventory.Items.GetCountById(Charm));
        Assert.AreEqual(ShardsPerWolfPouch, inventory.Items.GetCountById(Shard));
        Assert.AreEqual(0, inventory.Items.GetCountById(WolfPouch));
        character.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void WolfPouch_InFullInventoryWithNonstackableTertiaryAndPouches_FitsBecauseInputsFreeSlots()
    {
        var (inventory, character, service) = CreateWolf(charmStackable: true, emptyPouchStackable: false);
        for (var i = 0; i < 6; i++) inventory.Items.Add(new StackItem(EmptyPouch, 1, stackable: false));
        for (var i = 0; i < 6; i++) inventory.Items.Add(new StackItem(WolfBones, 1, stackable: false));
        inventory.Items.Add(new StackItem(Charm, 6));
        inventory.Items.Add(new StackItem(Shard, 6 * ShardsPerWolfPouch));
        for (var i = 0; i < 14; i++) inventory.Items.Add(new StackItem(900 + i, 1, stackable: false));
        Assert.AreEqual(28, inventory.Items.TakenSlots);

        Assert.IsTrue(service.TryCreatePouches(character, Definition, WolfPouch, 6));

        Assert.AreEqual(6, inventory.Items.GetCountById(WolfPouch));
        Assert.AreEqual(0, inventory.Items.GetCountById(EmptyPouch));
        Assert.AreEqual(0, inventory.Items.GetCountById(WolfBones));
        Assert.AreEqual(0, inventory.Items.GetCountById(Charm));
        Assert.AreEqual(0, inventory.Items.GetCountById(Shard));
        character.Statistics.Received(1).AddExperience(Arg.Any<int>(), PouchXp * 6);
    }

    [TestMethod]
    public void WolfPouch_WhenAllRecipeInputsStackAndInventoryIsFull_RollsBackEveryInputAndGrantsNoExperience()
    {
        // Hypothetical stacked-everything stackability (cache stackability unverified): inputs free only four slots.
        var (inventory, character, service) = CreateWolf(charmStackable: true, emptyPouchStackable: true, tertiaryStackable: true);
        inventory.Items.Add(new StackItem(EmptyPouch, 10, stackable: true));
        inventory.Items.Add(new StackItem(WolfBones, 10, stackable: true));
        inventory.Items.Add(new StackItem(Charm, 10));
        inventory.Items.Add(new StackItem(Shard, 10 * ShardsPerWolfPouch));
        for (var i = 0; i < 24; i++) inventory.Items.Add(new StackItem(900 + i, 1, stackable: false));
        Assert.AreEqual(28, inventory.Items.TakenSlots);

        Assert.IsFalse(service.TryCreatePouches(character, Definition, WolfPouch, 10));

        Assert.AreEqual(10, inventory.Items.GetCountById(EmptyPouch));
        Assert.AreEqual(10, inventory.Items.GetCountById(WolfBones));
        Assert.AreEqual(10, inventory.Items.GetCountById(Charm));
        Assert.AreEqual(10 * ShardsPerWolfPouch, inventory.Items.GetCountById(Shard));
        Assert.AreEqual(0, inventory.Items.GetCountById(WolfPouch));
        Assert.AreEqual(28, inventory.Items.TakenSlots);
        character.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    private static (ComposedTestContainer Inventory, ICharacter Character, SummoningSkillService Service) CreateWolf(
        bool charmStackable, bool emptyPouchStackable, bool tertiaryStackable = false)
    {
        var (inventory, character, _) = Create();
        var definition = Substitute.For<IItemDefinition>();
        definition.ExtraData.Returns(new Dictionary<int, object>
        {
            [538] = EmptyPouch, [539] = 1, [540] = Charm, [541] = 1, [542] = Shard, [543] = ShardsPerWolfPouch, [544] = WolfBones, [545] = 1
        });
        var items = Substitute.For<IItemService>();
        items.FindItemDefinitionById(Arg.Any<int>()).Returns(definition);
        character.ServiceProvider.Returns(new ServiceCollection().AddSingleton(items).BuildServiceProvider());
        var stackableIds = new HashSet<int> { Shard };
        if (charmStackable) stackableIds.Add(Charm);
        if (emptyPouchStackable) stackableIds.Add(EmptyPouch);
        if (tertiaryStackable) stackableIds.Add(WolfBones);
        var builder = new TestItemBuilder((id, count) => new StackItem(id, count, stackableIds.Contains(id)));
        return (inventory, character,
            new SummoningSkillService(Substitute.For<INpcBuilder>(), Substitute.For<IFamiliarScriptProvider>(), builder));
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

    private static (ComposedTestContainer Inventory, ICharacter Character, SummoningSkillService Service) CreateStacked()
    {
        var (inventory, character, _) = Create();
        var builder = new TestItemBuilder((id, count) => new StackItem(id, count));
        return (inventory, character,
            new SummoningSkillService(Substitute.For<INpcBuilder>(), Substitute.For<IFamiliarScriptProvider>(), builder));
    }

    /// <summary>Item double with real mutable counts; charms and shards stack, everything else does not.</summary>
    private sealed class StackItem(int id, int count, bool? stackable = null) : IItem
    {
        public int Id { get; } = id;
        public long[] ExtraData => [];
        public int Count { get; set; } = count;
        public string Name => $"Item {Id}";
        public IItemDefinition ItemDefinition { get; } = CreateDefinition(stackable ?? (id == Charm || id == Shard));
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript { get; } = CreateScript(stackable ?? (id == Charm || id == Shard));
        public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
        public IItem Clone() => new StackItem(Id, Count, ItemDefinition.Stackable);
        public IItem Clone(int newCount) => new StackItem(Id, newCount, ItemDefinition.Stackable);
        public bool Equals(IItem otherItem, bool ignoreCount = true) => otherItem is not null && Id == otherItem.Id &&
            (ignoreCount || Count == otherItem.Count);
        public string? SerializeExtraData() => null;

        private static IItemDefinition CreateDefinition(bool stackable)
        {
            var definition = Substitute.For<IItemDefinition>();
            definition.Stackable.Returns(stackable);
            return definition;
        }

        private static IItemScript CreateScript(bool stackable)
        {
            var script = Substitute.For<IItemScript>();
            script.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(stackable);
            return script;
        }
    }
}
