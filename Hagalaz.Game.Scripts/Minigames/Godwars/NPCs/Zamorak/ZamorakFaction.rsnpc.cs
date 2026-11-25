
namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Zamorak
{
    [NpcScriptMetaData([6210, 6211, 6212, 6213, 6214, 6215, 6216, 6217, 6218, 6219, 6220, 6221])]
    public class ZamorakFaction(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : GodwarsFactionScript<ZamorakFaction>(owner, npcService, pathFinder, widgetScriptActivator)
    {
    }
}
