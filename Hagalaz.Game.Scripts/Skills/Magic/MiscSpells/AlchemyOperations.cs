using System;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Resources;

namespace Hagalaz.Game.Scripts.Skills.Magic.MiscSpells;

internal static class AlchemyOperations
{
    public static bool Cast(
        ICharacter caster,
        IItem item,
        IItemBuilder itemBuilder,
        int coinAmount,
        int animationId,
        int graphicId,
        int experience,
        Func<bool> checkRequirements,
        Action removeRunes)
    {
        if (caster.HasState<AlchingState>()) return false;
        if (!checkRequirements()) return false;

        var slot = caster.Inventory.Items.GetInstanceSlot(item);
        if (slot == -1) return false;

        var coins = itemBuilder.Create().WithId(995).WithCount(coinAmount).Build();
        if (!caster.Inventory.Items.HasSpaceFor(coins) && !caster.MoneyPouch.HasSpaceForCoins(coins.Count))
        {
            caster.SendChatMessage(GameStrings.InventoryFull);
            return false;
        }

        removeRunes();
        var removed = caster.Inventory.Items.Remove(itemBuilder.Create().WithId(item.Id).WithCount(1).Build(), slot);
        if (removed <= 0) return true;
        if (!caster.Inventory.Items.Add(coins)) return true;

        caster.QueueAnimation(Animation.Create(animationId));
        caster.QueueGraphic(Graphic.Create(graphicId));
        caster.Statistics.AddExperience(StatisticsConstants.Magic, experience);
        caster.Configurations.SendGlobalCs2Int(168, 7);
        caster.AddState(new AlchingState { TicksLeft = 2 });
        return true;
    }
}
