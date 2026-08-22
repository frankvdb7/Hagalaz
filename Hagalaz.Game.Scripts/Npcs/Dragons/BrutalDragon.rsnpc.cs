using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Dragons
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([5362])]
    public class BrutalDragon : StandardDragon
    {
        public BrutalDragon(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
        {
        }
    }
}
