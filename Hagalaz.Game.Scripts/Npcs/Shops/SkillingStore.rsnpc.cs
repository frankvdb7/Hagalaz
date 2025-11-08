using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Shops
{
    /// <summary>
    ///     Contains skilling store script.
    /// </summary>
    [NpcScriptMetaData([531])]
    public class SkillingStore : NpcScriptBase
    {
        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize() { }

        /// <summary>
        ///     Happens when character clicks NPC and then walks to it and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacter is overrided or/and
        ///     handles to click this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character that clicked this npc.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, NpcClickType clickType)
        {
            if (clickType == NpcClickType.Option3Click)
            {
                clicker.EventManager.SendEvent(new OpenShopEvent(clicker, 10));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }
    }
}