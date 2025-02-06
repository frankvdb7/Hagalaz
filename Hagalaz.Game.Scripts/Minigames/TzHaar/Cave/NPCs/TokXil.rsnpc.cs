using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

namespace Hagalaz.Game.Scripts.Minigames.TzHaar.Cave.NPCs
{
    [NpcScriptMetaData([2739, 2740])]
    public class TokXil : StandardCaveNpc
    {
        /// <summary>
        ///     The ranging.
        /// </summary>
        private bool _ranging = true;

        public TokXil(INpcBuilder npcBuilder) : base(npcBuilder) { }

        /// <summary>
        ///     Get's attack distance of this npc.
        ///     By default , this method does return Definition.AttackDistance
        /// </summary>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int GetAttackDistance() => 8;

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => _ranging ? AttackStyle.RangedAccurate : AttackStyle.MeleeAggressive;

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => _ranging ? AttackBonus.Ranged : AttackBonus.Crush;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            _ranging = !Owner.WithinRange(target, 1) || RandomStatic.Generator.Next(0, 100) >= 50;

            if (!_ranging)
            {
                base.PerformAttack(target);
                return;
            }

            // ranged

            Owner.QueueAnimation(Animation.Create(16132));

            var deltaX = Owner.Location.X - target.Location.X;
            var deltaY = Owner.Location.Y - target.Location.Y;
            if (deltaX < 0)
            {
                deltaX = -deltaX;
            }

            if (deltaY < 0)
            {
                deltaY = -deltaY;
            }

            var delay = (byte)(deltaX * 5 + deltaY * 5);
            var projectile = new Projectile(2993);
            projectile.SetSenderData(Owner, 50, false);
            projectile.SetReceiverData(target, 35);
            projectile.SetFlyingProperties(10, delay, 10, 0, false);
            projectile.Display();

            var preDmg = target.Combat.IncomingAttack(Owner, DamageType.StandardRange, ((INpcCombat)Owner.Combat).GetRangeDamage(target), 0);
            Owner.QueueTask(new RsTask(() =>
                {
                    var soaked = -1;
                    var damage = target.Combat.Attack(Owner, DamageType.StandardRange, preDmg, ref soaked);
                    var splat = new HitSplat(Owner);
                    splat.SetFirstSplat(damage == -1 ? HitSplatType.HitMiss : HitSplatType.HitRangeDamage, damage == -1 ? 0 : damage, ((INpcCombat)Owner.Combat).GetRangeMaxHit(target) <= damage);
                    if (soaked != -1)
                    {
                        splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soaked, false);
                    }

                    target.QueueHitSplat(splat);
                }, CreatureHelper.CalculateTicksForClientTicks(delay)));
        }
    }
}