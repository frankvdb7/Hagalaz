using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Skills.Magic.MiscSpells;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests.Skills.Magic;

[TestClass]
public sealed class SpellCastRegressionTests
{
    [DataTestMethod]
    [DataRow(false, 1963, "2,2,1", 15, 25.0)]
    [DataRow(true, 6883, "2,4,4", 60, 35.5)]
    public void BonesConversionSpell_CastChecksAndRemovesRunesBeforeConverting(bool peaches, int productId, string expectedRuneAmounts, int level, double xpPerBone)
    {
        var inventory = new ComposedTestContainer(8);
        inventory.SetItem(0, CreateMagicItem(526, 2));
        var character = Substitute.For<ICharacter>();
        character.Inventory.Returns(inventory);
        character.Statistics.Returns(Substitute.For<ICharacterStatistics>());
        var magic = Substitute.For<IMagic>();
        character.Magic.Returns(magic);
        var calls = new List<string>();
        var expectedRunes = peaches
            ? new[] { RuneType.Nature, RuneType.Water, RuneType.Earth }
            : new[] { RuneType.Earth, RuneType.Water, RuneType.Nature };
        var expectedAmounts = expectedRuneAmounts.Split(',').Select(int.Parse).ToArray();
        magic.CheckMagicLevel(level).Returns(_ => { calls.Add("level"); return true; });
        magic.CheckRunes(Arg.Any<RuneType[]>(), Arg.Any<int[]>()).Returns(call =>
        {
            calls.Add("runes");
            CollectionAssert.AreEqual(expectedRunes, call.ArgAt<RuneType[]>(0));
            CollectionAssert.AreEqual(expectedAmounts, call.ArgAt<int[]>(1));
            Assert.AreEqual(2, inventory.Items.GetCountById(526));
            return true;
        });
        magic.When(x => x.RemoveRunes(Arg.Any<RuneType[]>(), Arg.Any<int[]>())).Do(_ => calls.Add("remove"));
        var context = Substitute.For<ICharacterContext>(); context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>(); accessor.Context.Returns(context);
        Func<bool> cast = peaches ? new BonesToPeaches(CreateItemBuilder(), accessor).Cast : new BonesToBananas(CreateItemBuilder(), accessor).Cast;

        Assert.IsTrue(cast());

        CollectionAssert.AreEqual(new[] { "level", "runes", "remove" }, calls);
        Assert.AreEqual(2, inventory.Items.GetCountById(productId));
        character.Statistics.Received(1).AddExperience(StatisticsConstants.Magic, xpPerBone * 2);
    }

    [DataTestMethod]
    [DataRow(false, 526, 1963)]
    [DataRow(true, 532, 6883)]
    public void BonesConversionSpell_WhenRuneRequirementFailsDoesNotConsumeBones(bool peaches, int boneId, int productId)
    {
        var inventory = new ComposedTestContainer(3); var bones = CreateMagicItem(boneId); inventory.SetItem(0, bones);
        var character = Substitute.For<ICharacter>(); character.Inventory.Returns(inventory);
        var magic = Substitute.For<IMagic>(); character.Magic.Returns(magic);
        magic.CheckMagicLevel(Arg.Any<int>()).Returns(true); magic.CheckRunes(Arg.Any<RuneType[]>(), Arg.Any<int[]>()).Returns(false);
        var context = Substitute.For<ICharacterContext>(); context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>(); accessor.Context.Returns(context);
        Func<bool> cast = peaches ? new BonesToPeaches(CreateItemBuilder(), accessor).Cast : new BonesToBananas(CreateItemBuilder(), accessor).Cast;

        Assert.IsFalse(cast());
        Assert.AreSame(bones, inventory.Items[0]); Assert.AreEqual(0, inventory.Items.GetCountById(productId));
        magic.DidNotReceive().RemoveRunes(Arg.Any<RuneType[]>(), Arg.Any<int[]>());
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BonesConversionSpell_WithNoBonesStillConsumesRequirementsBeforeReturningFalse(bool peaches)
    {
        var character = Substitute.For<ICharacter>(); character.Inventory.Returns(new ComposedTestContainer(2));
        var magic = Substitute.For<IMagic>(); character.Magic.Returns(magic);
        magic.CheckMagicLevel(Arg.Any<int>()).Returns(true); magic.CheckRunes(Arg.Any<RuneType[]>(), Arg.Any<int[]>()).Returns(true);
        var context = Substitute.For<ICharacterContext>(); context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>(); accessor.Context.Returns(context);
        Func<bool> cast = peaches ? new BonesToPeaches(CreateItemBuilder(), accessor).Cast : new BonesToBananas(CreateItemBuilder(), accessor).Cast;

        Assert.IsFalse(cast());
        magic.Received(1).RemoveRunes(Arg.Any<RuneType[]>(), Arg.Any<int[]>());
        character.Received(1).SendChatMessage("You don't have any bones to cast this spell on.");
    }

    [DataTestMethod]
    [DataRow(false, 12345, 712, 112, 31)]
    [DataRow(true, 54321, 713, 113, 65)]
    public void Alchemy_CastUsesSpellSpecificValueEffectsAndState(bool high, int value, int animation, int graphic, int xp)
    {
        var inventory = new ComposedTestContainer(4); var source = CreateMagicItem(100);
        var definition = Substitute.For<IItemDefinition>();
        if (high) definition.HighAlchemyValue.Returns(value); else definition.LowAlchemyValue.Returns(value);
        ((MagicItem)source).SetDefinition(definition); inventory.SetItem(0, source);
        var character = CreateAlchemyCharacter(inventory, out var states);
        character.Magic.CheckMagicLevel(Arg.Any<int>()).Returns(true);
        character.Magic.CheckRunes(Arg.Any<RuneType[]>(), Arg.Any<int[]>()).Returns(true);
        var context = Substitute.For<ICharacterContext>(); context.Character.Returns(character);
        var accessor = Substitute.For<ICharacterContextAccessor>(); accessor.Context.Returns(context);
        var spell = high ? (object)new HighLevelAlchemy(CreateItemBuilder(), accessor) : new LowLevelAlchemy(CreateItemBuilder(), accessor);

        Assert.IsTrue(high ? ((HighLevelAlchemy)spell).Cast(source) : ((LowLevelAlchemy)spell).Cast(source));

        Assert.AreEqual(0, inventory.Items.GetCountById(100)); Assert.AreEqual(value, inventory.Items.GetCountById(995));
        character.Received(1).QueueAnimation(Arg.Is<IAnimation>(x => x.Id == animation));
        character.Received(1).QueueGraphic(Arg.Is<IGraphic>(x => x.Id == graphic));
        character.Statistics.Received(1).AddExperience(StatisticsConstants.Magic, xp);
        character.Configurations.Received(1).SendGlobalCs2Int(168, 7);
        Assert.IsInstanceOfType<AlchingState>(states.Single()); Assert.AreEqual(2, ((AlchingState)states.Single()).TicksLeft);
    }

    [TestMethod]
    public void Alchemy_RejectsExistingStateMissingItemAndNoDestinationCapacity()
    {
        var inventory = new ComposedTestContainer(2); var source = CreateMagicItem(100);
        source.ItemDefinition.HighAlchemyValue.Returns(500); inventory.SetItem(0, source);
        var character = CreateAlchemyCharacter(inventory, out var states); var accessor = CreateAccessor(character);
        var spell = new HighLevelAlchemy(CreateItemBuilder(), accessor);
        character.Magic.CheckMagicLevel(Arg.Any<int>()).Returns(true);
        character.Magic.CheckRunes(Arg.Any<RuneType[]>(), Arg.Any<int[]>()).Returns(true);
        character.HasState<AlchingState>().Returns(true);
        Assert.IsFalse(spell.Cast(source)); Assert.AreSame(source, inventory.Items[0]);
        Assert.AreEqual(0, inventory.Items.GetCountById(995));

        character.HasState<AlchingState>().Returns(false); inventory.Items.Remove(source, 0);
        Assert.IsFalse(spell.Cast(source));

        inventory.SetItem(0, source); inventory.SetItem(1, CreateMagicItem(200));
        character.MoneyPouch.HasSpaceForCoins(500).Returns(false);
        Assert.IsFalse(spell.Cast(source)); Assert.AreSame(source, inventory.Items[0]);
        character.Received(1).SendChatMessage("Not enough space in your inventory.");
        Assert.AreEqual(0, inventory.Items.GetCountById(995));
        character.Statistics.DidNotReceive().AddExperience(StatisticsConstants.Magic, Arg.Any<double>());
        Assert.AreEqual(0, states.Count);
    }

    private static ICharacter CreateAlchemyCharacter(ComposedTestContainer inventory, out List<IState> states)
    {
        states = [];
        var character = Substitute.For<ICharacter>(); character.Inventory.Returns(inventory);
        character.Statistics.Returns(Substitute.For<ICharacterStatistics>()); character.Magic.Returns(Substitute.For<IMagic>());
        character.Configurations.Returns(Substitute.For<IConfigurations>()); character.MoneyPouch.Returns(Substitute.For<IMoneyPouchContainer>());
        character.HasState<AlchingState>().Returns(false); character.AddState(Arg.Do<IState>(states.Add));
        return character;
    }

    private static ICharacterContextAccessor CreateAccessor(ICharacter character)
    { var context = Substitute.For<ICharacterContext>(); context.Character.Returns(character); var accessor = Substitute.For<ICharacterContextAccessor>(); accessor.Context.Returns(context); return accessor; }

    private static IItemBuilder CreateItemBuilder() => new TestItemBuilder(CreateMagicItem);

    private static IItem CreateMagicItem(int id, int count = 1) => new MagicItem(id, count);

    private sealed class MagicItem(int id, int count) : IItem
    {
        private IItemDefinition _definition = MakeDefinition(id);
        private readonly Hagalaz.Game.Abstractions.Model.Items.IItemScript _script = MakeScript();
        public int Id { get; } = id;
        public long[] ExtraData => [];
        public int Count { get; set; } = count;
        public string Name => $"Test item {Id}";
        public IItemDefinition ItemDefinition => _definition;
        public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
        public Hagalaz.Game.Abstractions.Model.Items.IItemScript ItemScript => _script;
        public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
        public IItem Clone() => new MagicItem(Id, Count) { _definition = _definition };
        public IItem Clone(int newCount) => new MagicItem(Id, newCount) { _definition = _definition };
        public bool Equals(IItem otherItem, bool ignoreCount = true) => otherItem is not null && Id == otherItem.Id && (ignoreCount || Count == otherItem.Count);
        public string? SerializeExtraData() => null;
        public void SetDefinition(IItemDefinition definition) => _definition = definition;
        private static IItemDefinition MakeDefinition(int itemId)
        { var d = Substitute.For<IItemDefinition>(); d.Stackable.Returns(itemId is 995 or 1963 or 6883); return d; }
        private static Hagalaz.Game.Abstractions.Model.Items.IItemScript MakeScript()
        { var s = Substitute.For<Hagalaz.Game.Abstractions.Model.Items.IItemScript>(); s.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(true); return s; }
    }
}
