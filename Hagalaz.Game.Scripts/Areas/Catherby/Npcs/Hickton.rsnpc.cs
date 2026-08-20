using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Catherby.Npcs
{
    [NpcScriptMetaData([575])]
    public class Hickton : NpcScriptBase
    {
        public Hickton(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
        {
        }
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
                clicker.EventManager.SendEvent(new OpenShopEvent(clicker, 20));
            }
            else if (clickType == NpcClickType.Option1Click)
            {
                var fletchingSkillDialogue = CreateWidgetScript<SkillCapeDialogue>(clicker);
                fletchingSkillDialogue.SkillID = StatisticsConstants.Fletching;
                clicker.Widgets.OpenDialogue(fletchingSkillDialogue, true, Owner);
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
            }
        }
    }
}
