using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
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

        public override string GetSpecialMoveDescription() => "Boosts your Defence level by 8.";

        protected override void ApplySpecialMoveEffect(IRuneObject target)
        {
            var statistics = Summoner.Statistics;
            var max = statistics.LevelForExperience(StatisticsConstants.Defence) + DefenceBoost;
            statistics.HealSkill(StatisticsConstants.Defence, max, DefenceBoost);
        }

        private const int DefenceBoost = 8;
    }
}
