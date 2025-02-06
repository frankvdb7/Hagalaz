using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model;
using Hagalaz.Game.Model.Combat;
using Hagalaz.Game.Utilities;

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

            var max = combat.GetMagicMaxHit(victim, GetBaseDamage(caster));
            var damage = combat.GetMagicDamage(victim, max);

            combat.PerformSoulSplit(victim, damage);

            damage = victim.Combat.IncomingAttack(caster, DamageType.FullMagic, damage, (byte)(51 + deltaX * 5 + deltaY * 5));
            combat.AddMagicExperience(damage);

            var delay = (byte)(51 + deltaX * 5 + deltaY * 5);
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

            caster.QueueTask(new RsTask(() =>
                {
                    var soak = -1;
                    var dmg = victim.Combat.Attack(caster, DamageType.FullMagic, damage, ref soak);
                    if (dmg != -1)
                    {
                        var splat = new HitSplat(caster);
                        splat.SetFirstSplat(HitSplatType.HitMagicDamage, dmg, dmg >= GetBaseDamage(caster));
                        if (soak != -1)
                        {
                            splat.SetSecondSplat(HitSplatType.HitDefendedDamage, soak, false);
                        }

                        victim.QueueHitSplat(splat);
                        OnSuccessfulHit(caster, victim);
                    }
                    else
                    {
                        victim.QueueGraphic(Graphic.Create(85, 0, 150));
                    }
                }, CreatureHelper.CalculateTicksForClientTicks(delay)));
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
        public virtual void OnSuccessfulHit(ICharacter caster, ICreature victim)
        {
        }

        /// <summary>
        ///     Renders the projectile.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        /// <param name="delay">The delay.</param>
        public virtual void RenderProjectile(ICharacter caster, ICreature victim, byte delay)
        {
            var projectile = new Projectile(Dto.ProjectileId);
            projectile.SetSenderData(caster, 43);
            projectile.SetReceiverData(victim, 25);
            projectile.SetFlyingProperties(51, (byte)(delay - 51), 16, 20, false);
            projectile.Display();
        }

        /// <summary>
        ///     Check's requirements for this spell.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public virtual bool CheckRequirements(ICharacter caster) => caster.Magic.CheckMagicLevel(Dto.RequiredLevel) && caster.Magic.CheckRunes(Dto.RequiredRunes, Dto.RequiredRunesCounts);

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