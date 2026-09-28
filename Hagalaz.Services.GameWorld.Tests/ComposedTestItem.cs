using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using NSubstitute;

namespace Hagalaz.Services.GameWorld.Tests;

internal sealed class ComposedTestItemBuilder : IItemBuilder, IItemId, IItemOptional
{
    private int _id;
    private int _count = 1;

    public IItemId Create() => this;
    public IItemOptional WithId(int id) { _id = id; return this; }
    public IItemOptional WithCount(int count) { _count = count; return this; }
    public IItemOptional WithExtraData(string data) => this;
    public IItem Build() => new ComposedTestItem(_id, _count, stackable: true);
}

internal sealed class ComposedTestItem : IItem
{
    public ComposedTestItem(int id, int count, bool stackable)
    {
        Id = id;
        Count = count;
        ItemDefinition = Substitute.For<IItemDefinition>();
        ItemDefinition.Stackable.Returns(stackable);
        ItemDefinition.Noted.Returns(false);
        ItemScript = Substitute.For<IItemScript>();
        ItemScript.CanBuyItem(Arg.Any<IItem>(), Arg.Any<ICharacter>()).Returns(true);
        ItemScript.CanStackItem(Arg.Any<IItem>(), Arg.Any<IItem>(), Arg.Any<bool>()).Returns(callInfo =>
        {
            var left = callInfo.ArgAt<IItem>(0);
            var right = callInfo.ArgAt<IItem>(1);
            return callInfo.ArgAt<bool>(2) || left.Id == right.Id && left.ItemDefinition.Stackable;
        });
    }

    private ComposedTestItem(int id, int count, IItemDefinition definition, IItemScript script) =>
        (Id, Count, ItemDefinition, ItemScript) = (id, count, definition, script);

    public int Id { get; }
    public int Count { get; set; }
    public string Name => $"Test item {Id}";
    public IItemDefinition ItemDefinition { get; }
    public IEquipmentDefinition EquipmentDefinition { get; } = Substitute.For<IEquipmentDefinition>();
    public IItemScript ItemScript { get; }
    public IEquipmentScript EquipmentScript { get; } = Substitute.For<IEquipmentScript>();
    public long[] ExtraData => [];
    public IItem Clone() => new ComposedTestItem(Id, Count, ItemDefinition, ItemScript);
    public IItem Clone(int newCount) => new ComposedTestItem(Id, newCount, ItemDefinition, ItemScript);
    public bool Equals(IItem otherItem, bool ignoreCount = true) =>
        otherItem != null && Id == otherItem.Id && (ignoreCount || Count == otherItem.Count);
    public string? SerializeExtraData() => null;
}
