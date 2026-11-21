using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Saradomin
{
    [NpcScriptMetaData([6254, 6255, /*6256,*/ /*6257,*/ 6258, 6259])]
    public class SaradominFaction(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : GodwarsFactionScript<SaradominFaction>(owner, npcService, pathFinder, widgetScriptActivator)
    {
    }
}
