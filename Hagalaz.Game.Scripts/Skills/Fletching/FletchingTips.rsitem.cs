﻿using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    /// <summary>
    /// Represents a script for the fletching tips within the game.
    /// This class is responsible for handling the functionality when interacting with fletching tip items.
    /// </summary>
    public class FletchingTips : ItemScript
    {
        private readonly IFletchingSkillService _fletchingSkillService;

        public FletchingTips(IFletchingSkillService fletchingSkillService) => _fletchingSkillService = fletchingSkillService;

        /// <summary>
        ///     Uses the item on an other item.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///     <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character) => _fletchingSkillService.TryFletchTips(character, used, usedWith);
    }
}