using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Prayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Prayer;

[TestClass]
public sealed class StandardPrayerItemRegressionTests
{
    [TestMethod]
    public async Task StandardBones_BuriesAndAwardsExperienceAfterDefinitionAndInstanceLookup()
    {
        var result = await RunPrayerItemAsync(526, ashes: false);

        Assert.IsInstanceOfType<BuryingBonesState>(result.State);
        AssertCharacterMessage(result.Character, "You dig a hole in the ground.");
        ((BuryingBonesState)result.State).OnRemoved(result.Character);
        Assert.AreEqual(0, result.Inventory.Items.GetCountById(526));
        result.Character.Received(1).SendChatMessage("You bury the bones.");
        result.Character.Statistics.Received(1).AddExperience(StatisticsConstants.Prayer, 4.5);
    }

    [TestMethod]
    public async Task StandardAsh_ScattersWithoutBuryMessageAndAwardsAshExperience()
    {
        var result = await RunPrayerItemAsync(592, ashes: true);

        Assert.IsInstanceOfType<BuryingBonesState>(result.State);
        result.Character.DidNotReceive().SendChatMessage("You dig a hole in the ground.");
        ((BuryingBonesState)result.State).OnRemoved(result.Character);
        Assert.AreEqual(0, result.Inventory.Items.GetCountById(592));
        result.Character.Received(1).SendChatMessage("You scatter the ashes.");
        result.Character.Statistics.Received(1).AddExperience(StatisticsConstants.Prayer, 4.5);
    }

    private static async Task<(ICharacter Character, ComposedTestContainer Inventory, IState State)> RunPrayerItemAsync(
        int itemId, bool ashes)
    {
        var inventory = new ComposedTestContainer(2);
        var item = ComposedTestContainer.CreateTestItem(itemId);
        inventory.SetItem(0, item);
        var statistics = Substitute.For<ICharacterStatistics>();
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        character.Statistics.Returns(statistics);
        IState? capturedState = null;
        character.AddState(Arg.Do<IState>(state => capturedState = state));
        Func<CancellationToken, Task>? operation = null;
        character.QueueTask(Arg.Do<Func<CancellationToken, Task>>(task => operation = task))
            .Returns(Substitute.For<IRsTaskHandle>());
        var service = Substitute.For<IPrayerService>();
        service.FindById(itemId).Returns(Task.FromResult<PrayerDto?>(new PrayerDto
        {
            ItemId = itemId,
            Experience = 4.5,
            Type = ashes ? PrayerDtoType.Ashes : PrayerDtoType.Bones
        }));

        if (ashes) new StandardAsh(service).ItemClickedInInventory(ComponentClickType.LeftClick, item, character);
        else new StandardBones(service).ItemClickedInInventory(ComponentClickType.LeftClick, item, character);
        await operation!(CancellationToken.None);

        Assert.IsNotNull(capturedState);
        return (character, inventory, capturedState);
    }

    private static void AssertCharacterMessage(ICharacter character, string message) => character.Received(1).SendChatMessage(message);
}
