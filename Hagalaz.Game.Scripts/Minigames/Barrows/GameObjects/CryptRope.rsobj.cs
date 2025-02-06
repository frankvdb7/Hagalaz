using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Dialogues.Generic;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.GameObjects
{
    [GameObjectScriptMetaData([6709, 6710, 6711])]
    public class CryptRope : GameObjectScript
    {
        /// <summary>
        ///     Happens when character click's this object and then walks to it
        ///     and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacterClick is overrided
        ///     than this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                var yesNoDialogue = clicker.ServiceProvider.GetRequiredService<YesNoDialogueScript>();
                yesNoDialogue.Question = "Would you like to leave the crypts?";
                yesNoDialogue.Callback = yes =>
                {
                    if (yes)
                    {
                        clicker.Movement.Lock(true);
                        clicker.QueueAnimation(Animation.Create(828));
                        clicker.QueueTask(new RsTask(() =>
                            {
                                clicker.Movement.Teleport(Teleport.Create(Location.Create(3571, 3311, 0, 0))); // house
                                clicker.Movement.Unlock(false);
                            }, 2));
                    }
                    clicker.Widgets.CloseChatboxOverlay();
                };
                clicker.Widgets.OpenDialogue(yesNoDialogue, false);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}