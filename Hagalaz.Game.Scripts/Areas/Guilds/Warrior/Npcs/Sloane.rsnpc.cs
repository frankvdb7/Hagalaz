using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue;

namespace Hagalaz.Game.Scripts.Areas.Guilds.Warrior.Npcs
{
    [NpcScriptMetaData([4297])]
    public class Sloane(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : SkillCapeMasterScript(owner, npcService, pathFinder, widgetScriptActivator, StatisticsConstants.Strength)
    {
    }
}
