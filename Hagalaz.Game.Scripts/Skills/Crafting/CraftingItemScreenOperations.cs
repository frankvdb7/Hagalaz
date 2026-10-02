using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Scripts.Skills.Crafting;

internal static class CraftingItemScreenOperations
{
    public static bool HandleMakeClick(
        ICharacter character,
        int requiredLevel,
        ComponentClickType clickType,
        Func<int> getMaximumCount,
        Action<int> start,
        Action<OnIntInput?> setInputHandler)
    {
        if (character.Statistics.GetSkillLevel(StatisticsConstants.Crafting) < requiredLevel)
        {
            character.SendChatMessage("You need a crafting level of " + requiredLevel + " to create that.");
            return false;
        }

        var count = clickType switch
        {
            ComponentClickType.LeftClick => 1,
            ComponentClickType.Option2Click => 5,
            ComponentClickType.Option3Click => getMaximumCount(),
            _ => 0
        };

        if (clickType == ComponentClickType.Option4Click)
        {
            OnIntInput handler = value =>
            {
                setInputHandler(null);
                if (value <= 0)
                {
                    character.SendChatMessage("Value can't be negative.");
                }
                else
                {
                    start(value);
                }
            };

            setInputHandler(handler);
            character.Configurations.SendIntegerInput("Please enter the amount to make:");
            return true;
        }

        start(count);
        return true;
    }
}
