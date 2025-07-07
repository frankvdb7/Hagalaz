using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Zaros
{
    /// <summary>
    ///     Contains blood reaver npc script.
    /// </summary>
    [NpcScriptMetaData([13458])]
    public class BloodReaver : NpcScriptBase
    {
        private readonly IProjectileBuilder _projectileBuilder;

        public BloodReaver(IProjectileBuilder projectileBuilder)
        {
            _projectileBuilder = projectileBuilder;
        }

        /// <summary>
        ///     Render's attack.
        /// </summary>
        public override void RenderAttack() => Owner.QueueAnimation(Animation.Create(7004));

        /// <summary>
        ///     Render's defence.
        /// </summary>
        public override void RenderDefence(int delay) => Owner.QueueAnimation(Animation.Create(7028, delay));

        /// <summary>
        ///     Perform's attack to specific target.
        /// </summary>
        public override void PerformAttack(ICreature target)
        {
            RenderAttack();

            _projectileBuilder.Create()
                .WithGraphicId(374)
                .FromCreature(Owner)
                .ToCreature(target)
                .WithDuration(30)
                .WithAngle(192)
                .WithSlope(15)
                .WithDelay(50)
                .WithFromHeight(11)
                .WithToHeight(15)
                .Send();

            Owner.Combat.PerformAttack(new AttackParams()
            {
                Damage = ((INpcCombat)Owner.Combat).GetMagicDamage(target, 146),
                MaxDamage = ((INpcCombat)Owner.Combat).GetMagicMaxHit(target, 146),
                DamageType = DamageType.StandardMagic,
                Delay = 30,
                Target = target
            });

            target.QueueGraphic(Graphic.Create(375, 30));
        }


        /// <summary>
        ///     Render's this npc death.
        /// </summary>
        /// <returns></returns>
        public override int RenderDeath()
        {
            Owner.QueueAnimation(Animation.Create(7000));
            return 7;
        }

        /// <summary>
        ///     Get's attack distance of this reaver.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackDistance() => 10;

        /// <summary>
        ///     Get's attack speed of this reaver.
        /// </summary>
        /// <returns></returns>
        public override int GetAttackSpeed() => 4;

        /// <summary>
        ///     Get's attack style of this reaver.
        /// </summary>
        /// <returns></returns>
        public override AttackStyle GetAttackStyle() => AttackStyle.MeleeAggressive;

        /// <summary>
        ///     Get's attack bonus of this reaver.
        /// </summary>
        /// <returns></returns>
        public override AttackBonus GetAttackBonusType() => AttackBonus.Magic;
    }
}