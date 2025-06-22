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
    [NpcScriptMetaData([2743, 2744])]
    public class KetZek : StandardCaveNpc
    {
        private readonly IProjectileBuilder _projectileBuilder;

        /// <summary>
        ///     The ranging.
        /// </summary>
        private bool _maging = true;

        public KetZek(INpcBuilder npcBuilder, IProjectileBuilder projectileBuilder) : base(npcBuilder)
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
        public override AttackStyle GetAttackStyle() => _maging ? AttackStyle.MagicNormal : AttackStyle.MeleeAggressive;

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => _maging ? AttackBonus.Magic : AttackBonus.Crush;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            _maging = !Owner.WithinRange(target, 1) || RandomStatic.Generator.Next(0, 100) >= 50;

            if (!_maging)
            {
                base.PerformAttack(target);
                return;
            }

            // magic

            Owner.QueueAnimation(Animation.Create(16136));

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
            _projectileBuilder.Create().WithGraphicId(2984)
                .FromCreature(Owner)
                .ToCreature(target)
                .WithDuration(delay)
                .WithFromHeight(75)
                .WithToHeight(35)
                .WithDelay(25)
                .WithSlope(10)
                .Send();

            target.QueueGraphic(Graphic.Create(2983, delay));
            
            Owner.Combat.PerformAttack(new AttackParams()
            {
                Target = target,
                DamageType = DamageType.StandardMagic,
                Damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 490),
                MaxDamage = ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, 490),
                Delay = delay
            });
        }
    }
}