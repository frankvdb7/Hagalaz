using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Godwars;

internal static class GodSwordAssembly
{
    internal static bool TryAssemble(IItemBuilder itemBuilder, IItem used, IItem usedWith, ICharacter character,
        int resultId)
    {
        using var transaction = ItemContainerTransaction.Begin(character.Inventory);
        var inventory = character.Inventory;
        var usedSlot = inventory.Items.GetInstanceSlot(used);
        if (usedSlot == -1) return false;

        var usedWithSlot = inventory.Items.GetInstanceSlot(usedWith);
        if (usedWithSlot == -1) return false;

        if (!inventory.TryRemoveItemExact(used, usedSlot)) return false;
        inventory.ReplaceItem(usedWithSlot, itemBuilder.Create().WithId(resultId).Build());
        transaction.Commit();
        inventory.PublishChanges();
        return true;
    }
}
