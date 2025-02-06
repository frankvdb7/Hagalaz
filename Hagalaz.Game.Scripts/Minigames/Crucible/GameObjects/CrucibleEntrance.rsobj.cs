using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Scripts.Dialogues.Generic;
using Hagalaz.Game.Scripts.Model.GameObjects;
using Hagalaz.Game.Scripts.Widgets.Warning;

namespace Hagalaz.Game.Scripts.Minigames.Crucible.GameObjects
{
    [GameObjectScriptMetaData([67052])]
    public class SecondEntrance : GameObjectScript
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
                if (clicker.Statistics.FullCombatLevel < 60)
                {
                    var infoDialogue = clicker.ServiceProvider.GetRequiredService<InfoDialogueScript>();
                    infoDialogue.Texts = ["You need to be at least level combat level 60 to enter the Crucible."];
                    clicker.Widgets.OpenDialogue(infoDialogue, true);
                    return;
                }

                var warningScript = clicker.ServiceProvider.GetRequiredService<WarningInterfaceScript>();
                warningScript.OnAcceptClicked = () =>
                {
                    Crucible.TeleportToBank(clicker);
                };
                warningScript.AcceptComponentID = 12;
                warningScript.DeclineComponentID = 13;
                clicker.Widgets.OpenWidget(1292, 0, warningScript, true);
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