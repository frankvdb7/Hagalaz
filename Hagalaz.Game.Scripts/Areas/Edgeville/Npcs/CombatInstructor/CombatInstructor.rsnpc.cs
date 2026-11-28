using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.CombatInstructor
{
    [NpcScriptMetaData([944])]
    public class CombatInstructor(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : DialogueNpcScript<CombatInstructorDialogue>(owner, npcService, pathFinder, widgetScriptActivator, interrupt: false)
    {
    }
}
