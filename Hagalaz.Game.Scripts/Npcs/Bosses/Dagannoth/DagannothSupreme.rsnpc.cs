using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Bosses.Dagannoth
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([2881])]
    public class DagannothSupreme : NpcScriptBase
    {
        private readonly IProjectileBuilder _projectileBuilder;

        public DagannothSupreme(IProjectileBuilder projectileBuilder)
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
        public override int GetAttackDistance() => 7;

        /// <summary>
        ///     Get's attack style of this npc.
        ///     By default , this method does return AttackStyle.Accurate.
        /// </summary>
        /// <returns>
        ///     AttackStyle.
        /// </returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.RangedAccurate;

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Ranged;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();
            var combat = (INpcCombat)Owner.Combat;

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

            var delay = 20 + deltaX * 5 + deltaY * 5;

            _projectileBuilder.Create()
                .WithGraphicId(1206)
                .FromCreature(Owner)
                .ToCreature(target)
                .WithDuration(delay)
                .WithFromHeight(35)
                .WithToHeight(35)
                .WithAngle(180)
                .WithDelay(50)
                .Send();

            Owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = ((INpcCombat)Owner.Combat).GetRangeDamage(target),
                MaxDamage = ((INpcCombat)Owner.Combat).GetRangeMaxHit(target),
                Delay = delay,
                DamageType = DamageType.StandardRange,
                Target = target
            });
        }
    }
}