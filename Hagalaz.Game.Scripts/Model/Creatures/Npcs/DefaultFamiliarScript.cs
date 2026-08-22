using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Services;

namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    /// Default script for summoned npcs.
    /// </summary>
    public class DefaultFamiliarScript : FamiliarScriptBase
    {
        public DefaultFamiliarScript(ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService, IItemBuilder itemBuilder, IWidgetScriptActivator widgetScriptActivator) : base(pathFinder, npcService, itemService, itemBuilder, widgetScriptActivator) { }

    }
}
