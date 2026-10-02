using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Scripts.Items.Auras;

internal static class CosmeticAuraOperations
{
    internal static bool TryHandleEquipmentClick(ComponentClickType clickType, IItem aura, ICharacter character,
        int itemIdOffset)
    {
        if (clickType == ComponentClickType.Option2Click)
        {
            ToggleAura(character, aura, itemIdOffset);
            return true;
        }

        if (clickType == ComponentClickType.Option4Click)
        {
            aura.EquipmentScript.UnEquipItem(aura, character);
            return true;
        }

        return false;
    }

    internal static void ToggleAura(ICharacter character, IItem aura, int itemIdOffset)
    {
        var slot = character.Equipment.GetInstanceSlot(aura);
        if (slot == EquipmentSlot.NoSlot) return;

        var itemBuilder = character.ServiceProvider.GetRequiredService<IItemBuilder>();
        var replacement = itemBuilder.Create().WithId(aura.Id + itemIdOffset).WithCount(aura.Count).Build();
        character.Equipment.TryReplaceEquippedItem(slot, aura, replacement);
    }
}
