using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.MasterCrafter
{
    [NpcScriptMetaData([805])]
    public class MasterCrafter(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : SkillCapeMasterScript(owner, npcService, pathFinder, widgetScriptActivator, StatisticsConstants.Crafting)
    {
    }
}
