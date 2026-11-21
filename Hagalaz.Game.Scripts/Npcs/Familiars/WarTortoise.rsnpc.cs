using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Familiars
{
    [NpcScriptMetaData([6815, 6816])]
    public class WarTortoise(
        INpc owner, IItemContainerFactory itemContainerFactory, ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService,
        IGroundItemBuilder groundItemBuilder, IItemBuilder itemBuilder, IWidgetScriptActivator widgetScriptActivator)
        : SingleMoveBobFamiliarScriptBase(owner, itemContainerFactory, pathFinder, npcService, itemService, groundItemBuilder, itemBuilder, widgetScriptActivator)
    {
        public override int InventoryCapacity => 18;

        public override int GetRequiredSpecialMovePoints() => 20;

        public override FamiliarSpecialType GetSpecialType() => FamiliarSpecialType.Click;

        public override string GetSpecialMoveName() => "Testudo";

        // TODO - the move does not hit yet (max 80); it only consumes the scroll and points.
        public override string GetSpecialMoveDescription() => "Inflicts up to 80 damage against your opponent.";
    }
}
