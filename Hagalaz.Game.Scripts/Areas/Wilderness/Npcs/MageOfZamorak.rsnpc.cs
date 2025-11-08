using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Dialogues;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Wilderness.Npcs
{
    [NpcScriptMetaData([2257])]
    public class MageOfZamorak : NpcScriptBase
    {
        /// <summary>
        ///     Happens when character clicks NPC and then walks to it and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacter is overrided or/and
        ///     handles to click this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option1Click)
            {
                var script = clicker.ServiceProvider.GetRequiredService<MageOfZamorakDialogue>();
                clicker.Widgets.OpenDialogue(script, true, Owner);
                return;
            }

            if (clickType == NpcClickType.Option3Click)
            {
                clicker.EventManager.SendEvent(new OpenShopEvent(clicker, 16));
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