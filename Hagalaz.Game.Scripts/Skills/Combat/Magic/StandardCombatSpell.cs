using System;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Contains standart combat spell.
    /// </summary>
    public class StandardCombatSpell : DefinedCombatSpell
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="StandardCombatSpell" /> class.
        /// </summary>
        /// <param name="dto">The definition.</param>
        public StandardCombatSpell(CombatSpellDto dto) : base(dto) { }

        /// <summary>
        ///     Perform's attack to target.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        public override void PerformAttack(ICharacter caster, ICreature victim)
        {
            var combat = (ICharacterCombat)caster.Combat;
            var delay = (byte)BeginCast(caster, victim);
            var maxDamage = combat.GetMagicMaxHit(victim, GetBaseDamage(caster));
            var damage = combat.GetMagicDamage(victim, maxDamage);

            var attackHandle = caster.Combat.PerformAttack(new AttackParams()
            {
                Damage = damage,
                DamageType = DamageType.FullMagic,
                MaxDamage = maxDamage,
                Delay = delay,
                Target = victim.Handle
            });

            if (Dto.ProjectileId != -1)
            {
                RenderProjectile(caster, victim, delay);
            }

            if (damage == -1)
            {
                victim.QueueGraphic(Graphic.Create(85, delay, 150));
            }
            else if (Dto.EndGraphicId != -1)
            {
                victim.QueueGraphic(Graphic.Create(Dto.EndGraphicId, delay, 100));
            }

            attackHandle.RegisterResultHandler(result =>
            {
                if (result.DamageLifePoints.Succeeded)
                {
                    OnSuccessfulHit(caster, victim);
                }
                else
                {
                    victim.QueueGraphic(Graphic.Create(85, 0, 150));
                }
            });
        }

        /// <summary>
        ///     Plays the cast animation and graphic, grants the cast experience and returns the hit delay for the victim's distance.
        /// </summary>
        protected int BeginCast(ICharacter caster, ICreature victim)
        {
            caster.QueueAnimation(Animation.Create(Dto.CastAnimationId));
            caster.QueueGraphic(Graphic.Create(Dto.CastGraphicId));
            caster.Statistics.AddExperience(StatisticsConstants.Magic, Dto.BaseExperience);

            return 51 + Math.Abs(caster.Location.X - victim.Location.X) * 5 + Math.Abs(caster.Location.Y - victim.Location.Y) * 5;
        }

        /// <summary>
        ///     Called when [succesfull attack].
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        public virtual void OnSuccessfulHit(ICharacter caster, ICreature victim) { }

        /// <summary>
        ///     Renders the projectile.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        /// <param name="delay">The delay.</param>
        public virtual void RenderProjectile(ICharacter caster, ICreature victim, int delay)
        {
            var projectileBuilder = caster.ServiceProvider.GetRequiredService<IProjectileBuilder>();
            projectileBuilder.Create()
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

        /// <summary>
        ///     Gets the base damage.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        public virtual int GetBaseDamage(ICharacter caster) => Dto.BaseDamage;
    }
}
