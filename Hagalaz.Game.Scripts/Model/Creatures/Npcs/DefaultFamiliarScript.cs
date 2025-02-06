using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    /// Default script for summoned npcs.
    /// </summary>
    public class DefaultFamiliarScript : FamiliarScriptBase
    {
        public DefaultFamiliarScript(ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService) : base(pathFinder, npcService, itemService) { }

        protected override void InitializeFamiliar() { }

    }
}