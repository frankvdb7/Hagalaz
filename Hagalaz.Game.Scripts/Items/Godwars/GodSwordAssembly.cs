using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Godwars;

internal static class GodSwordAssembly
{
    internal static bool TryAssemble(IItemBuilder itemBuilder, IItem used, IItem usedWith, ICharacter character,
        int resultId)
    {
        var inventory = character.Inventory.Items;
        var usedSlot = inventory.GetInstanceSlot(used);
        if (usedSlot == -1) return false;

        var usedWithSlot = inventory.GetInstanceSlot(usedWith);
        if (usedWithSlot == -1) return false;

        inventory.Remove(used, usedSlot);
        inventory.Replace(usedWithSlot, itemBuilder.Create().WithId(resultId).Build());
        return true;
    }
}
