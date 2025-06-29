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
    public class StandardCombatSpell : ICombatSpell
    {
        /// <summary>
        ///     Contains the definition.
        /// </summary>
        public CombatSpellDto Dto { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="StandardCombatSpell" /> class.
        /// </summary>
        /// <param name="dto">The definition.</param>
        public StandardCombatSpell(CombatSpellDto dto) => Dto = dto;

        /// <summary>
        ///     Perform's attack to target.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        public virtual void PerformAttack(ICharacter caster, ICreature victim)
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

            var delay = (byte)(51 + deltaX * 5 + deltaY * 5);
            var maxDamage = combat.GetMagicMaxHit(victim, GetBaseDamage(caster));
            var damage = combat.GetMagicDamage(victim, maxDamage);

            var attackHandle = caster.Combat.PerformAttack(new AttackParams()
            {
                Damage = damage,
                DamageType = DamageType.FullMagic,
                MaxDamage = maxDamage,
                Delay = delay,
                Target = victim
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
        ///     Determines whether this instance can attack the specified caster.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        /// <returns></returns>
        public virtual bool CanAttack(ICharacter caster, ICreature victim) => true;

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
        ///     Check's requirements for this spell.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public virtual bool CheckRequirements(ICharacter caster) =>
            caster.Magic.CheckMagicLevel(Dto.RequiredLevel) && caster.Magic.CheckRunes(Dto.RequiredRunes, Dto.RequiredRunesCounts);

        /// <summary>
        ///     Removes required items from actor.
        /// </summary>
        /// <param name="caster"></param>
        public void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(Dto.RequiredRunes, Dto.RequiredRunesCounts);

        /// <summary>
        ///     Called when [auto casting activation].
        /// </summary>
        /// <param name="activatedOn">The activated on.</param>
        public void OnAutoCastingActivation(ICharacter activatedOn) => activatedOn.Configurations.SendStandardConfiguration(108, Dto.AutoCastConfig);

        /// <summary>
        ///     Called when [auto casting deactivation].
        /// </summary>
        /// <param name="deactivatedOn">The deactivated on.</param>
        public void OnAutoCastingDeactivation(ICharacter deactivatedOn) => deactivatedOn.Configurations.SendStandardConfiguration(108, -1);

        /// <summary>
        ///     Gets the casting speed.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public virtual int GetCastingSpeed(ICharacter caster) => 5;

        /// <summary>
        ///     Gets the base damage.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        public virtual int GetBaseDamage(ICharacter caster) => Dto.BaseDamage;

        /// <summary>
        ///     Gets the combat distance.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public int GetCombatDistance(ICharacter caster) => 8;

        /// <summary>
        ///     Gets the magic experience.
        /// </summary>
        /// <returns></returns>
        public double GetMagicExperience() => Dto.BaseExperience;
    }
}