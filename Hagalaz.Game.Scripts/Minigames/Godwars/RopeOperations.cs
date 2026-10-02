using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model;

namespace Hagalaz.Game.Scripts.Minigames.Godwars;

internal static class RopeOperations
{
    public static void AddRope(ICharacter character, IItemBuilder itemBuilder, IState ropeState, int varpBitFileId)
    {
        if (!character.Inventory.Items.Contains(954))
        {
            character.SendChatMessage("You need a rope in order to climb down here.");
            return;
        }

        character.QueueAnimation(Animation.Create(827));
        character.AddState(ropeState);
        character.Inventory.Items.Remove(itemBuilder.Create().WithId(954).Build());
        character.Configurations.SendBitConfiguration(varpBitFileId, 1);
    }
}
