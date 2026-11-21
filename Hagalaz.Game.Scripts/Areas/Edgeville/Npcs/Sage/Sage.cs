using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Sage
{
    [NpcScriptMetaData([2244])]
    public class Sage(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : DialogueNpcScript<SageDialogue>(owner, npcService, pathFinder, widgetScriptActivator)
    {
    }
}
