using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Items.Rings
{
    /// <summary>
    /// </summary>
    public class RingOfSlayingDialogue : ItemDialogueScript
    {
        /// <summary>
        ///     The equipment
        /// </summary>
        private readonly bool _equipment;

        public RingOfSlayingDialogue(ICharacterContextAccessor characterContextAccessor, bool equipment = false) : base(characterContextAccessor) => _equipment = equipment;

        /// <summary>
        ///     Initializes the dialogue.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                var nameArray = Item.Name.Split('(');
                int.TryParse(nameArray[1].Replace("(", "").Replace(")", ""), out var charges);
                StandardOptionDialogue(charges + " Charges Remaining", "Sumona", "Slayer Tower", "Fremennik Slayer Dungeon", "Tarn's Lair");
                return true;
            });

            AttachDialogueOptionClickHandler("Sumona", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportRingOfSlaying(Owner, Item, _equipment, Jewelry.Jewelry.RingOfSlayingTeleports[0]);
                return true;
            });

            AttachDialogueOptionClickHandler("Slayer Tower", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportRingOfSlaying(Owner, Item, _equipment, Jewelry.Jewelry.RingOfSlayingTeleports[1]);
                return true;
            });

            AttachDialogueOptionClickHandler("Fremennik Slayer Dungeon", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportRingOfSlaying(Owner, Item, _equipment, Jewelry.Jewelry.RingOfSlayingTeleports[2]);
                return true;
            });

            AttachDialogueOptionClickHandler("Tarn's Lair", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportRingOfSlaying(Owner, Item, _equipment, Jewelry.Jewelry.RingOfSlayingTeleports[3]);
                return true;
            });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }
    }
}