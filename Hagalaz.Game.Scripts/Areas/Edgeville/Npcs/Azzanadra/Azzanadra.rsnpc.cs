using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Azzanadra
{
    [NpcScriptMetaData([9047])]
    public class Azzanadra(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : DialogueNpcScript<AzzanadraDialogue>(owner, npcService, pathFinder, widgetScriptActivator)
    {
    }
}
