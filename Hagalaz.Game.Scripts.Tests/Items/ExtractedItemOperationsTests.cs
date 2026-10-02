using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Items.Godwars;
using Hagalaz.Game.Scripts.Minigames.Godwars;
using Hagalaz.Game.Scripts.Skills.Magic.MiscSpells;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Items;

[TestClass]
public sealed class ExtractedItemOperationsTests
{
    [TestMethod]
    public void GodSwordAssembly_ReplacesHiltAndRemovesExactShardInstances()
    {
        var inventory = new ComposedTestContainer(4);
        var character = CreateCharacter(inventory);
        var shard = ComposedTestContainer.CreateTestItem(11712);
        var hilt = ComposedTestContainer.CreateTestItem(11704);
        inventory.Items.Add(shard);
        inventory.Items.Add(hilt);
        var builder = CreateItemBuilder();

        var assembled = GodSwordAssembly.TryAssemble(builder, shard, hilt, character, 11690);

        Assert.IsTrue(assembled);
        Assert.IsNull(inventory.Items[0]);
        Assert.AreEqual(11690, inventory.Items[1]!.Id);
        Assert.AreEqual(1, inventory.Items.TakenSlots);
    }

    [TestMethod]
    public void GodSwordAssembly_WhenEitherExactSourceIsMissing_DoesNotMutateInventory()
    {
        var inventory = new ComposedTestContainer(2);
        var character = CreateCharacter(inventory);
        var shard = ComposedTestContainer.CreateTestItem(11712);
        var hilt = ComposedTestContainer.CreateTestItem(11704);
        inventory.Items.Add(hilt);

        Assert.IsFalse(GodSwordAssembly.TryAssemble(CreateItemBuilder(), shard, hilt, character, 11690));

        Assert.AreSame(hilt, inventory.Items[0]);
        Assert.AreEqual(1, inventory.Items.TakenSlots);
    }

    [TestMethod]
    public void RopeOperations_WithoutRope_ReportsRequirementAndLeavesStateUnchanged()
    {
        var inventory = new ComposedTestContainer(2);
        var character = CreateCharacter(inventory);
        var ropeState = Substitute.For<IState>();

        RopeOperations.AddRope(character, CreateItemBuilder(), ropeState, 123);

        character.Received(1).SendChatMessage("You need a rope in order to climb down here.");
        character.DidNotReceive().AddState(ropeState);
        character.DidNotReceive().QueueAnimation(Arg.Any<Hagalaz.Game.Abstractions.Model.IAnimation>());
        Assert.AreEqual(0, inventory.Items.TakenSlots);
    }

    [TestMethod]
    public void RopeOperations_WithRopeConsumesItAndAddsTraversalState()
    {
        var inventory = new ComposedTestContainer(2);
        var rope = ComposedTestContainer.CreateTestItem(954);
        inventory.Items.Add(rope);
        var character = CreateCharacter(inventory);
        var ropeState = Substitute.For<IState>();
        var configurations = Substitute.For<IConfigurations>();
        character.Configurations.Returns(configurations);
        var itemBuilder = new TestItemBuilder((id, count) => id == 954 ? rope : ComposedTestContainer.CreateTestItem(id, count));

        RopeOperations.AddRope(character, itemBuilder, ropeState, 123);

        character.Received(1).AddState(ropeState);
        character.Received(1).QueueAnimation(Arg.Any<Hagalaz.Game.Abstractions.Model.IAnimation>());
        configurations.Received(1).SendBitConfiguration(123, 1);
        Assert.AreEqual(0, inventory.Items.GetCountById(954));
    }

    [TestMethod]
    public void BonesConversion_WhenNoBones_ReturnsFalseWithoutOutputOrExperience()
    {
        var inventory = new ComposedTestContainer(4);
        var character = CreateCharacter(inventory);

        var converted = BonesConversion.TryConvert(character, CreateItemBuilder(), 1963, 25);

        Assert.IsFalse(converted);
        Assert.AreEqual(0, inventory.Items.GetCountById(1963));
        character.Received(1).SendChatMessage("You don't have any bones to cast this spell on.");
        character.DidNotReceive().QueueAnimation(Arg.Any<Hagalaz.Game.Abstractions.Model.IAnimation>());
        character.Statistics.DidNotReceive().AddExperience(StatisticsConstants.Magic, Arg.Any<double>());
    }

    [DataTestMethod]
    [DataRow(1963, 25.0)]
    [DataRow(6883, 35.5)]
    public void BonesConversion_ConsumesSupportedBonesAndAwardsSpellOutput(int productId, double experiencePerBone)
    {
        var inventory = new ComposedTestContainer(5);
        var ordinaryBones = CreateComparableItem(526, 2);
        var bigBones = CreateComparableItem(532, 1);
        inventory.Items.Add(ordinaryBones);
        inventory.Items.Add(bigBones);
        var character = CreateCharacter(inventory);
        var statistics = Substitute.For<ICharacterStatistics>();
        character.Statistics.Returns(statistics);
        var itemBuilder = new TestItemBuilder((id, count) => CreateComparableItem(id, count));

        var converted = BonesConversion.TryConvert(character, itemBuilder, productId, experiencePerBone);

        Assert.IsTrue(converted);
        Assert.AreEqual(0, inventory.Items.GetCountById(526));
        Assert.AreEqual(0, inventory.Items.GetCountById(532));
        Assert.AreEqual(3, inventory.Items.GetCountById(productId));
        statistics.Received(1).AddExperience(StatisticsConstants.Magic, experiencePerBone * 3);
        character.Received(1).QueueAnimation(Arg.Is<Hagalaz.Game.Abstractions.Model.IAnimation>(animation => animation.Id == 722));
        character.Received(1).QueueGraphic(Arg.Is<Hagalaz.Game.Abstractions.Model.IGraphic>(graphic => graphic.Id == 141));
    }

    private static ICharacter CreateCharacter(ComposedTestContainer inventory)
    {
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        character.Statistics.Returns(Substitute.For<ICharacterStatistics>());
        return character;
    }

    private static IItemBuilder CreateItemBuilder() => new TestItemBuilder(ComposedTestContainer.CreateTestItem);

    private static IItem CreateComparableItem(int id, int count)
        => new ComparableItem(id, count);

    private sealed class ComparableItem(int id, int count) : IItem
    {
        public int Id { get; } = id;
        public long[] ExtraData => [];
        public int Count { get; set; } = count;
        public string Name => $"Test item {Id}";
        public IItemDefinition ItemDefinition { get; } = Substitute.For<IItemDefinition>();
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public IItemScript ItemScript { get; } = Substitute.For<IItemScript>();
        public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
        public IItem Clone() => new ComparableItem(Id, Count);
        public IItem Clone(int newCount) => new ComparableItem(Id, newCount);
        public bool Equals(IItem otherItem, bool ignoreCount = true) => otherItem is not null && Id == otherItem.Id &&
            (ignoreCount || Count == otherItem.Count) && ExtraData.SequenceEqual(otherItem.ExtraData);
        public string? SerializeExtraData() => null;
    }
}
