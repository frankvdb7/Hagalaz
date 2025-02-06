using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Items.Rings
{
    /// <summary>
    /// </summary>
    public class RingOfDuelingDialogue : ItemDialogueScript
    {
        public RingOfDuelingDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Initializes the dialogue.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                var nameArray = Item.Name.Split('(');
                var charges = 1;
                int.TryParse(nameArray[1].Replace("(", "").Replace(")", ""), out charges);
                StandardOptionDialogue(charges + " Charges Remaining", "Duel Arena", "Castle Wars", "Mobilising Armies", "Fist Of Guthix");
                return true;
            });

            AttachDialogueOptionClickHandler("Duel Arena", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportRingOfDueling(Owner, Item, false, Jewelry.Jewelry.RingOfDuelingTeleports[0]);
                return true;
            });

            AttachDialogueOptionClickHandler("Castle Wars", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportRingOfDueling(Owner, Item, false, Jewelry.Jewelry.RingOfDuelingTeleports[1]);
                return true;
            });

            AttachDialogueOptionClickHandler("Mobilising Armies", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportRingOfDueling(Owner, Item, false, Jewelry.Jewelry.RingOfDuelingTeleports[2]);
                return true;
            });

            AttachDialogueOptionClickHandler("Fist Of Guthix", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportRingOfDueling(Owner, Item, false, Jewelry.Jewelry.RingOfDuelingTeleports[3]);
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