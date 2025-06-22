using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Builders.Projectile;
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
        private readonly IProjectileBuilder _projectileBuilder;

        /// <summary>
        ///     The ranging.
        /// </summary>
        private bool _ranging = true;

        public TokXil(INpcBuilder npcBuilder, IProjectileBuilder projectileBuilder) : base(npcBuilder)
        {
            _projectileBuilder = projectileBuilder;
        }

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

            var delay = deltaX * 5 + deltaY * 5;
            _projectileBuilder.Create()
                .WithGraphicId(2993)
                .FromCreature(Owner)
                .ToCreature(target)
                .WithDuration(delay)
                .WithFromHeight(50)
                .WithToHeight(35)
                .WithDelay(10)
                .WithSlope(10)
                .Send();

            Owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = ((INpcCombat)Owner.Combat).GetRangeDamage(target),
                DamageType = DamageType.StandardRange, 
                Target = target,
                MaxDamage = ((INpcCombat)Owner.Combat).GetRangeMaxHit(target),
                Delay = delay
            });
        }
    }
}