using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.MasterFisher
{
    [NpcScriptMetaData([308])]
    public class MasterFisher(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : SkillCapeMasterScript(owner, npcService, pathFinder, widgetScriptActivator, StatisticsConstants.Fishing)
    {
    }
}
