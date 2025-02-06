using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Dragons
{
    /// <summary>
    ///     Contains a dragon script.
    /// </summary>
    [NpcScriptMetaData([
        941, 4677, 4678, 4679, 4680, 5362, 10604, 10605, 10606, 10607, 10608, 10609, 55, 4681, 4682, 4683, 4684, 5178, 53, 4669, 4670, 4671, 4672, 10815,
        10816, 10817, 10818, 10819, 10820, 54, 4673, 4674, 4675, 4676, 10219, 10220, 10221, 10222, 10223, 10224
    ])]
    public class StandardDragon : NpcScriptBase
    {
        /// <summary>
        ///     The attack bonus.
        /// </summary>
        protected AttackBonus Bonus = AttackBonus.Slash;

        /// <summary>
        ///     The style.
        /// </summary>
        protected AttackStyle Style = AttackStyle.MeleeAggressive;

        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize() => Owner.AddState(new State(StateType.NpcTypeDragon, int.MaxValue));

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            if (RandomStatic.Generator.Next(0, 100) >= 80)
            {
                Bonus = AttackBonus.Magic;
                Style = AttackStyle.MagicNormal;

                const int maxHit = 550;
                // Dragon fire
                Owner.QueueAnimation(Animation.Create(14245));
                Owner.QueueGraphic(Graphic.Create(2465));

                var preDmg = target.Combat.IncomingAttack(Owner, DamageType.DragonFire, ((INpcCombat)Owner.Combat).GetMagicDamage(target, maxHit), 0);
                Owner.QueueTask(new RsTask(() =>
                    {
                        target.QueueGraphic(Graphic.Create(439));
                        var soaked = -1;
                        var damage = target.Combat.Attack(Owner, DamageType.DragonFire, preDmg, ref soaked);
                        var splat = new HitSplat(Owner);
                        splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitMagicDamage, damage == -1 ? 0 : damage, maxHit <= damage);
                        if (soaked != -1)
                        {
                            splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                        }

                        target.QueueHitSplat(splat);
                    },
                    2));
            }
            else
            {
                Style = AttackStyle.MeleeAggressive;
                Bonus = AttackBonus.Slash;
                base.PerformAttack(target);
            }
        }

        /// <summary>
        ///     Get's attack distance of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackDistance() => 1;

        /// <summary>
        ///     Get's attack speed of this npc.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackSpeed() => 5;

        /// <summary>
        ///     Get's attack style of this npc.
        /// </summary>
        /// <returns></returns>
        public override AttackStyle GetAttackStyle() => Style;

        /// <summary>
        ///     Get's attack bonus of this npc.
        /// </summary>
        /// <returns></returns>
        public override AttackBonus GetAttackBonusType() => Bonus;
    }
}