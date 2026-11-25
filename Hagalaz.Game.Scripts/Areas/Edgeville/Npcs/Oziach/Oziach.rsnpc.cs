using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Oziach
{
    [NpcScriptMetaData([747])]
    public class Oziach(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : DialogueNpcScript<OziachDialogue>(owner, npcService, pathFinder, widgetScriptActivator, interrupt: false)
    {
    }
}
