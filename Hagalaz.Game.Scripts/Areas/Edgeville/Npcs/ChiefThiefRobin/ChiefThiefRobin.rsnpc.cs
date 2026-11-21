using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Dialogues;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.ChiefThiefRobin
{
    [NpcScriptMetaData([11280])]
    public class ChiefThiefRobin(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : DialogueNpcScript<SellStolenGoodsDialogue>(owner, npcService, pathFinder, widgetScriptActivator)
    {
    }
}
