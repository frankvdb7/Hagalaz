using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Items.Necklaces
{
    /// <summary>
    /// </summary>
    public class GamesNecklaceDialogue : ItemDialogueScript
    {
        public GamesNecklaceDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

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
                StandardOptionDialogue(charges + " Charges Remaining", "Corporeal Beast", "Gamers' Grotto");
                return true;
            });

            AttachDialogueOptionClickHandler("Corporeal Beast", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportGamesNecklace(Owner, Item, false, Jewelry.Jewelry.GameNecklaceTeleports[1]);
                return true;
            });

            AttachDialogueOptionClickHandler("Gamers' Grotto", (extraData1, extraData2) =>
            {
                Jewelry.Jewelry.TeleportGamesNecklace(Owner, Item, false, Jewelry.Jewelry.GameNecklaceTeleports[0]);
                return true;
            });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() { }
    }
}