using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using NSubstitute;

namespace Hagalaz.Game.Scripts.Tests;

/// <summary>Shared inventory/performer scaffolding for skill task tests.</summary>
internal static class SkillTaskTestSupport
{
    public static void RemoveAll(ComposedTestContainer inventory, int id, int limit = int.MaxValue)
    {
        for (var slot = 0; slot < 28 && limit > 0; slot++)
        {
            if (inventory.Items[slot]?.Id != id) continue;
            inventory.Items.Remove(inventory.Items[slot]!, slot);
            limit--;
        }
    }

    public static (ComposedTestContainer Inventory, ICharacter Performer, TestItemBuilder ItemBuilder,
        ICharacterContextAccessor ContextAccessor) CreateScenario(params int[] itemIds)
    {
        var inventory = new ComposedTestContainer(28);
        foreach (var id in itemIds) inventory.Items.Add(CreateItem(id, 1));

        var performer = Substitute.For<ICharacter>();
        performer.Inventory.Returns(inventory);
        var context = Substitute.For<ICharacterContext>();
        context.Character.Returns(performer);
        var contextAccessor = Substitute.For<ICharacterContextAccessor>();
        contextAccessor.Context.Returns(context);

        return (inventory, performer, new TestItemBuilder(CreateItem), contextAccessor);
    }

    /// <summary>Item double with id-based equality and count-aware cloning, matching real item semantics.</summary>
    public static IItem CreateItem(int id, int count)
    {
        var item = Substitute.For<IItem>();
        item.Id.Returns(id);
        item.Count.Returns(count);
        var definition = Substitute.For<IItemDefinition>();
        definition.Stackable.Returns(false);
        item.ItemDefinition.Returns(definition);
        item.Equals(Arg.Any<IItem>(), Arg.Any<bool>()).Returns(call => call.ArgAt<IItem>(0).Id == id);
        item.Clone().Returns(_ => CreateItem(id, count));
        item.Clone(Arg.Any<int>()).Returns(call => CreateItem(id, call.Arg<int>()));
        return item;
    }
}
