using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Bandos
{
    [NpcScriptMetaData([6278])]
    public class SpiritualMage(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
        : BandosFaction(owner, npcService, pathFinder, widgetScriptActivator)
    {
        public override AttackBonus GetAttackBonusType() => AttackBonus.Magic;
        public override int GetAttackDistance() => 8;
        public override AttackStyle GetAttackStyle() => AttackStyle.MagicNormal;
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();
            SpiritualAttacks.PerformMagicAttack(Owner, target);
        }
    }
}
