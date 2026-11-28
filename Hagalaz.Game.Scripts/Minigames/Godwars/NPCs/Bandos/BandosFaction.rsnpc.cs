
namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Bandos
{
    [NpcScriptMetaData([6268, 6269, 6270, 6271, 6272, 6273, 6274, 6275, /*6276,*/ 6277, /*6278,*/ 6279, 6280, 6281, 6282, 6283, 9185, 374])]
    public class BandosFaction(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : GodwarsFactionScript<BandosFaction>(owner, npcService, pathFinder, widgetScriptActivator)
    {
    }
}
