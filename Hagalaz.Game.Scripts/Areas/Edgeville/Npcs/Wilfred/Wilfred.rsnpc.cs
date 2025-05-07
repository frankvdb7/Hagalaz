using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Wilfred
{
    [ItemScriptMetaData([4906])]
    public class Wilfred : NpcScriptBase
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
                var woodcuttingSkillCape = clicker.ServiceProvider.GetRequiredService<SkillCapeDialogue.SkillCapeDialogue>();
                woodcuttingSkillCape.SkillID = StatisticsConstants.Woodcutting;
                clicker.Widgets.OpenDialogue(woodcuttingSkillCape, true, Owner);
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
            }
        }
    }
}