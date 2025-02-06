using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Items.Amulets
{
    /// <summary>
    /// </summary>
    public class AmuletOfGloryDialogue : ItemDialogueScript
    {
        public AmuletOfGloryDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

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
                StandardOptionDialogue(charges + " Charges Remaining", "Edgeville", "Karamja", "Draynor Village", "Al-Kharid");
                return true;
            });

            AttachDialogueOptionClickHandler("Edgeville", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportAmuletOfGlory(Owner, Item, false, Jewelry.Jewelry.GloryTeleports[0]);
                return true;
            });

            AttachDialogueOptionClickHandler("Karamja", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportAmuletOfGlory(Owner, Item, false, Jewelry.Jewelry.GloryTeleports[1]);
                return true;
            });

            AttachDialogueOptionClickHandler("Draynor Village", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportAmuletOfGlory(Owner, Item, false, Jewelry.Jewelry.GloryTeleports[2]);
                return true;
            });

            AttachDialogueOptionClickHandler("Al-Kharid", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportAmuletOfGlory(Owner, Item, false, Jewelry.Jewelry.GloryTeleports[3]);
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