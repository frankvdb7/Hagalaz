using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Herblore.Herbs;
using NSubstitute;
using static Hagalaz.Game.Scripts.Tests.SkillTaskTestSupport;

namespace Hagalaz.Game.Scripts.Tests.Skills.Herblore;

[TestClass]
public sealed class HerbloreMakePotionTests
{
    private const int Vial = 3008;
    private const int Product = 15325;
    private const double Experience = 260;
    private const int Spikes = 12539;

    private static readonly PotionDto Potion = new()
    {
        PotionId = Product, Kind = PotionKind.Standard, UnfinishedPotionId = Vial, RequiredLevel = 1, Experience = Experience,
        PrimaryItemIds = [], SecondaryItemIds = [Spikes], SecondaryItemCount = 5
    };

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    [DataRow(4)]
    public void ExtremeRanging_WithFewerThanFiveSpikes_ChangesNothingAndGrantsNothing(int spikeCount)
    {
        var (inventory, performer, service) = CreateScenarioWithSpikes(spikeCount);

        var made = service.TryMakePotionOnce(performer, CreateItem(Vial, 1), CreateItem(Spikes, 1), Potion, false);

        Assert.IsFalse(made);
        Assert.AreEqual(spikeCount, inventory.Items.GetCountById(Spikes));
        Assert.AreEqual(1, inventory.Items.GetCountById(Vial));
        Assert.AreEqual(0, inventory.Items.GetCountById(Product));
        performer.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    [TestMethod]
    public void ExtremeRanging_WithFiveSpikes_ConsumesFiveAndGrantsOnePotionAndExperience()
    {
        var (inventory, performer, service) = CreateScenarioWithSpikes(5);

        var made = service.TryMakePotionOnce(performer, CreateItem(Vial, 1), CreateItem(Spikes, 1), Potion, false);

        Assert.IsTrue(made);
        Assert.AreEqual(0, inventory.Items.GetCountById(Spikes));
        Assert.AreEqual(0, inventory.Items.GetCountById(Vial));
        Assert.AreEqual(1, inventory.Items.GetCountById(Product));
        performer.Statistics.Received(1).AddExperience(Arg.Any<int>(), Experience);
    }

    [TestMethod]
    public void ExtremeRanging_WithSixSpikesAndTwoVials_OnlyFirstIterationSucceeds()
    {
        var (inventory, performer, service) = CreateScenarioWithSpikes(6);
        inventory.Items.Add(CreateItem(Vial, 1));

        var first = service.TryMakePotionOnce(performer, CreateItem(Vial, 1), CreateItem(Spikes, 1), Potion, false);
        var second = service.TryMakePotionOnce(performer, CreateItem(Vial, 1), CreateItem(Spikes, 1), Potion, false);

        Assert.IsTrue(first);
        Assert.IsFalse(second);
        Assert.AreEqual(1, inventory.Items.GetCountById(Spikes));
        Assert.AreEqual(1, inventory.Items.GetCountById(Vial));
        Assert.AreEqual(1, inventory.Items.GetCountById(Product));
        performer.Statistics.Received(1).AddExperience(Arg.Any<int>(), Experience);
    }

    [TestMethod]
    public void StandardPotion_ConsumesOneSecondaryIngredientAndTheUnfinishedPotion()
    {
        var (inventory, performer, itemBuilder, _) = CreateScenario(91, 221, 221);
        var attack = new PotionDto
        {
            PotionId = 121, Kind = PotionKind.Standard, UnfinishedPotionId = 91, RequiredLevel = 1, Experience = 25,
            PrimaryItemIds = [249], SecondaryItemIds = [221], SecondaryItemCount = 1
        };

        var made = new HerbloreSkillService(itemBuilder).TryMakePotionOnce(performer, CreateItem(91, 1), CreateItem(221, 1), attack, false);

        Assert.IsTrue(made);
        Assert.AreEqual(1, inventory.Items.GetCountById(221));
        Assert.AreEqual(0, inventory.Items.GetCountById(91));
        Assert.AreEqual(1, inventory.Items.GetCountById(121));
        performer.Statistics.Received(1).AddExperience(Arg.Any<int>(), 25);
    }

    [TestMethod]
    public void UnfinishedPotion_ConsumesOnePrimaryIngredientEvenWhenTheSecondaryCountIsHigher()
    {
        var (inventory, performer, itemBuilder, _) = CreateScenario(HerbloreConstants.FilledVial, 249, 249);
        var recipe = Potion with { UnfinishedPotionId = 91, PrimaryItemIds = [249] };

        var made = new HerbloreSkillService(itemBuilder).TryMakePotionOnce(performer, CreateItem(HerbloreConstants.FilledVial, 1), CreateItem(249, 1), recipe, true);

        Assert.IsTrue(made);
        Assert.AreEqual(1, inventory.Items.GetCountById(249));
        Assert.AreEqual(1, inventory.Items.GetCountById(91));
        performer.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    private static (ComposedTestContainer Inventory, ICharacter Performer, HerbloreSkillService Service)
        CreateScenarioWithSpikes(int spikeCount)
    {
        var (inventory, performer, itemBuilder, _) = CreateScenario(Vial);
        for (var i = 0; i < spikeCount; i++) inventory.Items.Add(CreateItem(Spikes, 1));

        return (inventory, performer, new HerbloreSkillService(itemBuilder));
    }
}
