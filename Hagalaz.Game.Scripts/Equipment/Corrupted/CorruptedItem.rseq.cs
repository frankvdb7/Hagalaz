using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Dialogues.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Corrupted
{
    public class CorruptedItem : EquipmentScript
    {
        /// <summary>
        ///     Get's called when specific item is about to be equiped.
        /// </summary>
        /// <param name="item">Item in character's inventory which should be equiped.</param>
        /// <param name="character">Character which should equip the item.</param>
        /// <returns>
        ///     <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public override bool EquipItem(IItem item, ICharacter character)
        {
            var degradeScript = character.ServiceProvider.GetRequiredService<DegradeDialogue>();
            character.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.DestroyItemBox, 0, degradeScript, true, item);
            return false;
        }

        /// <summary>
        ///     Get's items for which this script is made.
        /// </summary>
        /// <returns>
        ///     Return's array of item ids for which this script is suitable.
        /// </returns>
        public override IEnumerable<int> GetSuitableItems() =>
        [
            13908, 13911, 13914, 13917, 13920, 13923, 13926, 13929, 13932, 13935, 13938, 13941, 13944, 13947, 13950, // corrupted ancient equipment 
            13958, 13961, 13964, 13967, 13970, 13973, 13976, 13979, 13982, 13985, 13988 // corrupted dragon equipment
        ];
    }
}