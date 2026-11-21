using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model;
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

        public override string GetSpecialMoveDescription() => "Boosts your Agility level by 2 and restores run energy by half your Agility level.";

        protected override void ApplySpecialMoveEffect(IRuneObject target)
        {
            var statistics = Summoner.Statistics;
            var agility = statistics.LevelForExperience(StatisticsConstants.Agility);
            statistics.HealSkill(StatisticsConstants.Agility, agility + AgilityBoost, AgilityBoost);
            statistics.HealRunEnergy((agility + 1) / 2);
        }

        private const int AgilityBoost = 2;
    }
}
