using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Barrows.NPCs
{
    [NpcScriptMetaData([2025])]
    public class AhrimTheBlighted : BarrowBrother
    {
        private readonly IProjectileBuilder _projectileBuilder;

        public AhrimTheBlighted(IProjectileBuilder projectileBuilder) => _projectileBuilder = projectileBuilder;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            Owner.QueueAnimation(Animation.Create(14223));
            Owner.QueueGraphic(Graphic.Create(457));

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

            var delay = 51 + deltaX * 5 + deltaY * 5;

            _projectileBuilder.Create()
                .WithGraphicId(156)
                .FromCreature(Owner)
                .ToCreature(target)
                .WithDuration(delay)
                .WithDelay(51)
                .WithSlope(16)
                .Send();


            var damage = combat.GetMagicDamage(target, 160);

            var handler = combat.PerformAttack(new AttackParams()
            {
                Target = target, DamageType = DamageType.StandardMagic, Damage = damage, MaxDamage = damage, Delay = delay
            });


            handler.RegisterResultHandler(result =>
            {
                target.QueueGraphic(result.DamageLifePoints.Succeeded ? Graphic.Create(1019) : Graphic.Create(85, 0, 150));
            });
        }

        /// <summary>
        ///     Get's attack bonus type of this npc.
        ///     By default , this method does return AttackBonus.Crush
        /// </summary>
        /// <returns>
        ///     AttackBonus.
        /// </returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Magic;

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
        public override AttackStyle GetAttackStyle() => AttackStyle.MagicNormal;
    }
}