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
    [NpcScriptMetaData([6794, 6795])]
    public class SpiritTerrorBird(
        INpc owner, IItemContainerFactory itemContainerFactory, ISmartPathFinder pathFinder, INpcService npcService, IItemService itemService,
        IGroundItemBuilder groundItemBuilder, IItemBuilder itemBuilder, IWidgetScriptActivator widgetScriptActivator)
        : SingleMoveBobFamiliarScriptBase(owner, itemContainerFactory, pathFinder, npcService, itemService, groundItemBuilder, itemBuilder, widgetScriptActivator)
    {
        public override int InventoryCapacity => 12;

        public override int GetRequiredSpecialMovePoints() => 8;

        public override FamiliarSpecialType GetSpecialType() => FamiliarSpecialType.Click;

        public override string GetSpecialMoveName() => "Tireless Run";

        public override string GetSpecialMoveDescription() => "Inflicts up to 80 damage against your opponent.";
    }
}
