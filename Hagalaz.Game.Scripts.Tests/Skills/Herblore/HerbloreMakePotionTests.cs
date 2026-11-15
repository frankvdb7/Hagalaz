using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Scripts.Skills.Herblore.Herbs;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static Hagalaz.Game.Scripts.Tests.SkillTaskTestSupport;

namespace Hagalaz.Game.Scripts.Tests.Skills.Herblore;

[TestClass]
public sealed class HerbloreMakePotionTests
{
    private const int Vial = 3008;
    private const int Product = 15325;
    private const double Experience = 260;
    private static readonly int Spikes = HerbloreConstants.Grenwallspikes;
    private static readonly PotionDto Potion = new(Product, Vial, 1, Experience, [], [12539]);

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

    private static (ComposedTestContainer Inventory, ICharacter Performer, HerbloreSkillService Service)
        CreateScenarioWithSpikes(int spikeCount)
    {
        var (inventory, performer, itemBuilder, _) = CreateScenario(Vial);
        for (var i = 0; i < spikeCount; i++) inventory.Items.Add(CreateItem(Spikes, 1));

        var services = new ServiceCollection().AddSingleton(Substitute.For<IHerbloreService>()).BuildServiceProvider();
        return (inventory, performer, new HerbloreSkillService(services, itemBuilder));
    }
}
