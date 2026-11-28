using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Mandrith
{
    [NpcScriptMetaData([6537])]
    public class Mandrith(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : DialogueNpcScript<MandrithDialogue>(owner, npcService, pathFinder, widgetScriptActivator)
    {
    }
}
