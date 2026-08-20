using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Zaros
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([13451])]
    public class Fumus : NpcScriptBase
    {
        public Fumus(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
        {
        }
        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}
