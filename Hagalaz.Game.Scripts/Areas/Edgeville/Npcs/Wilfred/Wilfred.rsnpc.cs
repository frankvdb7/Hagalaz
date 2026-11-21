using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Wilfred
{
    [ItemScriptMetaData([4906])]
    public class Wilfred(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : SkillCapeMasterScript(owner, npcService, pathFinder, widgetScriptActivator, StatisticsConstants.Woodcutting)
    {
    }
}
