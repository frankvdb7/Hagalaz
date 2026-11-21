using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Skills.Slayer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Widgets;

[TestClass]
public sealed class RewardsBuyScreenTests
{
    [TestMethod]
    [DataRow(26, 75)]
    [DataRow(28, 35)]
    [DataRow(37, 35)]
    [DataRow(39, 35)]
    public void ItemReward_WithExactPoints_GrantsRewardAndDebitsOnce(int component, int cost)
    {
        var harness = new Harness(cost);

        Assert.IsTrue(harness.Click(component));

        Assert.AreEqual(0, harness.Balance);
        Assert.IsTrue(harness.AddedItems.Count > 0);
        // A repeated click finds no points left: nothing more is granted and the balance stays put.
        var granted = harness.AddedItems.Count;
        Assert.IsFalse(harness.Click(component));
        Assert.AreEqual(0, harness.Balance);
        Assert.AreEqual(granted, harness.AddedItems.Count);
    }

    [TestMethod]
    [DataRow(26, 74)]
    [DataRow(28, 34)]
    [DataRow(37, 0)]
    [DataRow(39, 0)]
    public void ItemReward_WithInsufficientOrMissingPoints_GrantsNothing(int component, int points)
    {
        var harness = new Harness(points);

        Assert.IsFalse(harness.Click(component));

        Assert.AreEqual(points, harness.Balance);
        Assert.AreEqual(0, harness.AddedItems.Count);
    }

    [TestMethod]
    [DataRow(26)]
    [DataRow(28)]
    [DataRow(37)]
    [DataRow(39)]
    public void ItemReward_WithFullInventory_KeepsPointsAndGrantsNothing(int component)
    {
        var harness = new Harness(100, hasSpace: false);

        Assert.IsFalse(harness.Click(component));

        Assert.AreEqual(100, harness.Balance);
        Assert.AreEqual(0, harness.AddedItems.Count);
    }

    [TestMethod]
    public void XpReward_GrantsOnceAndDebitsExactly()
    {
        var harness = new Harness(400);

        Assert.IsTrue(harness.Click(24));
        Assert.IsFalse(harness.Click(24));

        Assert.AreEqual(0, harness.Balance);
        harness.Statistics.Received(1).AddExperience(StatisticsConstants.Slayer, 10000);
    }

    [TestMethod]
    public void XpReward_WithInsufficientPoints_GrantsNothing()
    {
        var harness = new Harness(399);

        Assert.IsFalse(harness.Click(24));

        Assert.AreEqual(399, harness.Balance);
        harness.Statistics.DidNotReceive().AddExperience(Arg.Any<int>(), Arg.Any<double>());
    }

    private sealed class Harness
    {
        private readonly Dictionary<int, OnComponentClick> _handlers = new();

        public int Balance { get; private set; }
        public List<IItem> AddedItems { get; } = [];
        public ICharacterStatistics Statistics { get; } = Substitute.For<ICharacterStatistics>();

        public Harness(int balance, bool hasSpace = true)
        {
            Balance = balance;
            var profile = Substitute.For<IProfile>();
            profile.GetValue<int>(ProfileConstants.SlayerRewardPoints, Arg.Any<int>()).Returns(_ => Balance);
            profile.When(x => x.SetValue(ProfileConstants.SlayerRewardPoints, Arg.Any<int>()))
                .Do(call => Balance = call.ArgAt<int>(1));
            var items = Substitute.For<IReadOnlyItemContainer>();
            items.HasSpaceForRange(Arg.Any<IEnumerable<IItem?>>()).Returns(hasSpace);
            var inventory = Substitute.For<IInventoryContainer>();
            inventory.Items.Returns(items);
            inventory.AddItem(Arg.Any<IItem>()).Returns(call =>
            {
                AddedItems.Add(call.Arg<IItem>());
                return true;
            });
            var character = Substitute.For<ICharacter>();
            character.Profile.Returns(profile);
            character.Inventory.Returns(inventory);
            character.Statistics.Returns(Statistics);
            var context = Substitute.For<ICharacterContext>();
            context.Character.Returns(character);
            var accessor = Substitute.For<ICharacterContextAccessor>();
            accessor.Context.Returns(context);
            var widget = Substitute.For<IWidget>();
            widget.When(x => x.AttachClickHandler(Arg.Any<int>(), Arg.Any<OnComponentClick>()))
                .Do(call => _handlers[call.ArgAt<int>(0)] = call.ArgAt<OnComponentClick>(1));
            var screen = new RewardsBuyScreen(accessor, new TestItemBuilder(ComposedTestContainer.CreateTestItem));
            screen.Initialize(widget);
            screen.OnOpen();
        }

        public bool Click(int component) => _handlers[component](component, ComponentClickType.LeftClick, 0, 0);
    }
}
