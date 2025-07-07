using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     This class contains a script for the storm of armadyl combat spell.
    /// </summary>
    public class StormOfArmadyl : StandardCombatSpell
    {
        private readonly IProjectileBuilder _projectileBuilder;

        public StormOfArmadyl(CombatSpellDto dto, IProjectileBuilder projectileBuilder)
            : base(dto) =>
            _projectileBuilder = projectileBuilder;

        /// <summary>
        ///     Get's speed of this spell.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public override int GetCastingSpeed(ICharacter caster)
        {
            var weapon = caster.Equipment[EquipmentSlot.Weapon];
            return weapon?.Id == (int)StaffType.ArmadylBattleStaff ? 4 : 5;
        }

        /// <summary>
        ///     Perform's attack to specific target.
        /// </summary>
        /// <param name="caster"></param>
        /// <param name="victim"></param>
        public override void PerformAttack(ICharacter caster, ICreature victim)
        {
            caster.QueueAnimation(Animation.Create(Dto.CastAnimationId));
            caster.QueueGraphic(Graphic.Create(Dto.CastGraphicId));

            var combat = (ICharacterCombat)caster.Combat;

            caster.Statistics.AddExperience(StatisticsConstants.Magic, Dto.BaseExperience);

            var deltaX = caster.Location.X - victim.Location.X;
            var deltaY = caster.Location.Y - victim.Location.Y;
            if (deltaX < 0)
            {
                deltaX = -deltaX;
            }

            if (deltaY < 0)
            {
                deltaY = -deltaY;
            }

            var boost = (caster.Statistics.LevelForExperience(StatisticsConstants.Magic) - Dto.RequiredLevel) * 5;
            var max = combat.GetMagicMaxHit(victim, Dto.BaseDamage + boost);
            var damage = combat.GetMagicDamage(victim, max);
            if (damage > 0 && damage < boost)
            {
                damage += boost;
            }

            var delay = 51 + deltaX * 5 + deltaY * 5;
            RenderProjectile(caster, victim, delay);

            if (damage == -1)
            {
                victim.QueueGraphic(Graphic.Create(85, delay, 150));
            }
            else if (Dto.EndGraphicId != -1)
            {
                victim.QueueGraphic(Graphic.Create(Dto.EndGraphicId, delay));
            }

            var handle = caster.Combat.PerformAttack(new AttackParams()
            {
                Damage = damage,
                DamageType = DamageType.FullMagic,
                MaxDamage = max,
                Delay = delay,
                Target = victim
            });

            handle.RegisterResultHandler(_ => { victim.QueueGraphic(Graphic.Create(85, 0, 150)); });
        }

        /// <summary>
        ///     Renders the projectile.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        /// <param name="delay">The delay.</param>
        public override void RenderProjectile(ICharacter caster, ICreature victim, int delay) =>
            _projectileBuilder.Create()
                .WithGraphicId(Dto.ProjectileId)
                .FromCreature(caster)
                .ToCreature(victim)
                .WithDuration(delay - 51)
                .WithFromHeight(43)
                .WithToHeight(25)
                .WithDelay(51)
                .WithSlope(16)
                .WithAngle(20)
                .Send();
    }
}