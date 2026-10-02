using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Magic.MiscSpells;

internal static class BonesConversion
{
    public static bool TryConvert(ICharacter caster, IItemBuilder itemBuilder, int productId, double experiencePerItem)
    {
        var removed = caster.Inventory.Items.Remove(itemBuilder.Create().WithId(526).WithCount(caster.Inventory.Items.Capacity).Build());
        removed += caster.Inventory.Items.Remove(itemBuilder.Create().WithId(532).WithCount(caster.Inventory.Items.Capacity).Build());
        if (removed <= 0)
        {
            caster.SendChatMessage("You don't have any bones to cast this spell on.");
            return false;
        }

        caster.QueueAnimation(Animation.Create(722));
        caster.QueueGraphic(Graphic.Create(141, 0, 100));
        caster.Inventory.Items.Add(itemBuilder.Create().WithId(productId).WithCount(removed).Build());
        caster.Statistics.AddExperience(StatisticsConstants.Magic, experiencePerItem * removed);
        return true;
    }
}
