using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Saradomin
{
    [NpcScriptMetaData([6256])]
    public class SpiritualRanger(INpc owner, INpcAttackService attacks, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : SaradominFaction(owner, npcService, pathFinder, widgetScriptActivator)
    {
        public override AttackBonus GetAttackBonusType() => AttackBonus.Ranged;
        public override int GetAttackDistance() => 8;
        public override AttackStyle GetAttackStyle() => AttackStyle.RangedAccurate;
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();
            attacks.PerformRanged(Owner, target);
        }
    }
}
