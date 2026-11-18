using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Runecrafting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills;

[TestClass]
public sealed class RunecraftingAltarTests
{
    private const int AltarId = 100;
    private const int RuneId = 556;
    private const int RuneEssence = RunecraftingConstants.RuneEssence;
    private const int PureEssence = RunecraftingConstants.PureEssence;

    [TestMethod]
    public async Task PureOnlyAltar_WithBothEssenceTypes_ConsumesPureAndLeavesOrdinary()
    {
        var (inventory, character, altar) = Create(requiredLevel: 27, playerLevel: 30, ordinary: 1, pure: 2);

        await altar.CraftRunes(character);

        Assert.AreEqual(1, inventory.Items.GetCountById(RuneEssence));
        Assert.AreEqual(0, inventory.Items.GetCountById(PureEssence));
        Assert.AreEqual(2, inventory.Items.GetCountById(RuneId));
        character.Statistics.Received(1).AddExperience(StatisticsConstants.Runecrafting, 10d);
    }

    [TestMethod]
    public async Task PureOnlyAltar_WithOrdinaryOnly_RejectsWithoutMutation()
    {
        var (inventory, character, altar) = Create(requiredLevel: 27, playerLevel: 30, ordinary: 2, pure: 0);

        await altar.CraftRunes(character);

        Assert.AreEqual(2, inventory.Items.GetCountById(RuneEssence));
        Assert.AreEqual(0, inventory.Items.GetCountById(RuneId));
        character.Received(1).SendChatMessage(Arg.Is<string>(m => m.Contains("pure essence")));
        character.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public async Task PureOnlyAltar_WithPureOnly_Succeeds()
    {
        var (inventory, character, altar) = Create(requiredLevel: 27, playerLevel: 30, ordinary: 0, pure: 2);

        await altar.CraftRunes(character);

        Assert.AreEqual(0, inventory.Items.GetCountById(PureEssence));
        Assert.AreEqual(2, inventory.Items.GetCountById(RuneId));
    }

    [TestMethod]
    [DataRow(2, 0, RuneEssence)]
    [DataRow(0, 2, PureEssence)]
    public async Task BasicAltar_WithSingleEssenceType_Succeeds(int ordinary, int pure, int consumedId)
    {
        var (inventory, character, altar) = Create(requiredLevel: 1, playerLevel: 1, ordinary, pure);

        await altar.CraftRunes(character);

        Assert.AreEqual(0, inventory.Items.GetCountById(consumedId));
        Assert.AreEqual(2, inventory.Items.GetCountById(RuneId));
    }

    [TestMethod]
    public async Task BasicAltar_WithBothEssenceTypes_ConsumesOnlyOrdinary()
    {
        var (inventory, character, altar) = Create(requiredLevel: 1, playerLevel: 1, ordinary: 2, pure: 3);

        await altar.CraftRunes(character);

        Assert.AreEqual(0, inventory.Items.GetCountById(RuneEssence));
        Assert.AreEqual(3, inventory.Items.GetCountById(PureEssence));
        Assert.AreEqual(2, inventory.Items.GetCountById(RuneId));
    }

    [TestMethod]
    public async Task InsufficientLevel_ConsumesNothing()
    {
        var (inventory, character, altar) = Create(requiredLevel: 27, playerLevel: 26, ordinary: 1, pure: 2);

        await altar.CraftRunes(character);

        Assert.AreEqual(1, inventory.Items.GetCountById(RuneEssence));
        Assert.AreEqual(2, inventory.Items.GetCountById(PureEssence));
        Assert.AreEqual(0, inventory.Items.GetCountById(RuneId));
    }

    private static IItem CreateItem(int id, int count = 1)
    {
        var item = ComposedTestContainer.CreateTestItem(id, count);
        item.Equals(Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call => call.ArgAt<IItem>(0).Id == id);
        return item;
    }

    private static (ComposedTestContainer Inventory, ICharacter Character, Altar Altar) Create(
        int requiredLevel, int playerLevel, int ordinary, int pure)
    {
        var inventory = new ComposedTestContainer(28);
        var slot = 0;
        for (var i = 0; i < ordinary; i++) inventory.SetItem(slot++, CreateItem(RuneEssence));
        for (var i = 0; i < pure; i++) inventory.SetItem(slot++, CreateItem(PureEssence));

        var statistics = Substitute.For<ICharacterStatistics>();
        statistics.GetSkillLevel(StatisticsConstants.Runecrafting).Returns(playerLevel);
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        character.Statistics.Returns(statistics);

        var definition = new RunecraftingDto
        {
            AltarId = AltarId, RuinId = 0, PortalId = 0, RiftId = 0, RuneId = RuneId, TalismanId = 0, TiaraId = 0,
            RequiredLevel = requiredLevel, Experience = 5d, LevelCountMultipliers = [],
            AltarLocation = Substitute.For<ILocation>(), RuinLocation = Substitute.For<ILocation>(),
            RiftLocation = Substitute.For<ILocation>()
        };
        var service = Substitute.For<IRunecraftingService>();
        service.FindAltarById(AltarId).Returns(definition);

        var altar = new Altar(service, new TestItemBuilder(CreateItem));
        var owner = Substitute.For<IGameObject>();
        owner.Id.Returns(AltarId);
        altar.Initialize(owner);
        return (inventory, character, altar);
    }
}
