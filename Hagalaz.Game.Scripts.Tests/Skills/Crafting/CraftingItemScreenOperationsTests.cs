using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Crafting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Crafting;

[TestClass]
public sealed class CraftingItemScreenOperationsTests
{
    [TestMethod]
    public void HandleMakeClick_Option3UsesCallerSpecificMaximum()
    {
        var character = CreateCharacter();
        var jewelryCount = 4;
        var goldBarCount = 9;
        var jewelryStarted = 0;
        var silverBarCount = 7;
        var silverStarted = 0;

        CraftingItemScreenOperations.HandleMakeClick(character, 1, ComponentClickType.Option3Click,
            () => Math.Min(jewelryCount, goldBarCount), count => jewelryStarted = count, _ => { });
        CraftingItemScreenOperations.HandleMakeClick(character, 1, ComponentClickType.Option3Click,
            () => silverBarCount, count => silverStarted = count, _ => { });

        Assert.AreEqual(4, jewelryStarted);
        Assert.AreEqual(7, silverStarted);
    }

    [TestMethod]
    public void HandleMakeClick_Option4StartsRequestedPositiveCount()
    {
        var character = CreateCharacter();
        OnIntInput? inputHandler = null;
        var started = 0;

        var accepted = CraftingItemScreenOperations.HandleMakeClick(character, 1, ComponentClickType.Option4Click,
            () => 99, count => started = count, handler => inputHandler = handler);
        inputHandler!(12);

        Assert.IsTrue(accepted);
        Assert.AreEqual(12, started);
        Assert.IsNull(inputHandler);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-2)]
    public void HandleMakeClick_Option4RejectsNonpositiveInput(int input)
    {
        var character = CreateCharacter();
        OnIntInput? inputHandler = null;
        var startCalls = 0;

        CraftingItemScreenOperations.HandleMakeClick(character, 1, ComponentClickType.Option4Click,
            () => 99, _ => startCalls++, handler => inputHandler = handler);
        inputHandler!(input);

        Assert.AreEqual(0, startCalls);
        Assert.IsNull(inputHandler);
        character.Received(1).SendChatMessage("Value can't be negative.");
    }

    [TestMethod]
    public void HandleMakeClick_InsufficientLevelDoesNotStartTask()
    {
        var character = CreateCharacter(craftingLevel: 0);
        var startCalls = 0;

        var accepted = CraftingItemScreenOperations.HandleMakeClick(character, 1, ComponentClickType.LeftClick,
            () => 1, _ => startCalls++, _ => { });

        Assert.IsFalse(accepted);
        Assert.AreEqual(0, startCalls);
        character.Received(1).SendChatMessage("You need a crafting level of 1 to create that.");
    }

    private static ICharacter CreateCharacter(int craftingLevel = 99)
    {
        var character = Substitute.For<ICharacter>();
        var statistics = Substitute.For<ICharacterStatistics>();
        statistics.GetSkillLevel(StatisticsConstants.Crafting).Returns(craftingLevel);
        character.Statistics.Returns(statistics);
        return character;
    }
}
