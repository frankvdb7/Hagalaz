using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Widgets.Bank;

namespace Hagalaz.Game.Scripts.Npcs.Bankers
{
    [NpcScriptMetaData([902, 14707, 2617, 13455])]
    public class StandardBanker : NpcScriptBase
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
            if (clickType == NpcClickType.Option3Click)
            {
                var bankScript = clicker.ServiceProvider.GetRequiredService<BankScreen>();
                clicker.Widgets.OpenWidget(762, 0, bankScript, false);
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
            }
        }
    }
}