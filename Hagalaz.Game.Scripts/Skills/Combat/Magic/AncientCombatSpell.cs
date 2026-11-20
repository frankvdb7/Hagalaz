using System;
using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Shared casting flow of the Ancient blitz/burst/barrage/rush spell families
    ///     (spell types 0 = rush, 1 = burst, 2 = blitz, 3 = barrage). Types 1 and 3 hit every creature around the victim
    ///     in multi-combat areas. A family supplies its data tables and the effects that differ.
    /// </summary>
    public abstract class AncientCombatSpell : ICombatSpell
    {
        private const int SpellDelay = 51;

        protected AncientCombatSpell(int spellType, IProjectileBuilder projectileBuilder)
        {
            SpellType = spellType;
            ProjectileBuilder = projectileBuilder;
        }

        /// <summary>
        ///     Contains type of the spell.
        /// </summary>
        public int SpellType { get; }

        protected IProjectileBuilder ProjectileBuilder { get; }

        protected abstract RuneType[][] RuneRequirements { get; }

        protected abstract int[][] RuneAmounts { get; }

        protected abstract int[] LevelRequirements { get; }

        protected abstract int[] ConfigIds { get; }

        protected abstract double[] Experience { get; }

        protected abstract int[] BaseDamage { get; }

        /// <summary>
        ///     Whether additional multi-hit targets get the skull check. True unless the family opts out.
        /// </summary>
        protected virtual bool SkullsAdditionalTargets => true;

        /// <summary>
        ///     Performs the attack on the target.
        /// </summary>
        public void PerformAttack(ICharacter caster, ICreature victim)
        {
            var multiHit = SpellType is 1 or 3;
            RenderCast(caster);
            var combat = (ICharacterCombat)caster.Combat;

            caster.Statistics.AddExperience(StatisticsConstants.Magic, Experience[SpellType]);

            RenderCastEffects(caster, victim, GetTravelDuration(caster, victim));

            foreach (var c in caster.Viewport.VisibleCreatures)
            {
                if (c != victim)
                {
                    if (!multiHit || !caster.Area.MultiCombat || !victim.Area.MultiCombat || !c.Combat.CanBeAttackedBy(caster))
                    {
                        continue;
                    }

                    if (victim is ICharacter && !victim.Area.IsPvP || c is ICharacter && !c.Area.IsPvP)
                    {
                        continue;
                    }
                }

                if (c == caster || c.Location.X < victim.Location.X - 1 || c.Location.X > victim.Location.X + 1
                    || c.Location.Y < victim.Location.Y - 1 || c.Location.Y > victim.Location.Y + 1)
                {
                    continue;
                }

                // render skull if conditions are right
                if (c != victim && SkullsAdditionalTargets)
                {
                    combat.CheckSkullConditions(c);
                }

                var maxDamage = combat.GetMagicMaxHit(c, BaseDamage[SpellType]);
                var damage = combat.GetMagicDamage(c, maxDamage);
                var delay = SpellDelay + GetTravelDuration(caster, c);

                BeforeAttack(caster, c, damage, delay);

                var handle = caster.Combat.PerformAttack(new AttackParams()
                {
                    Damage = damage,
                    MaxDamage = maxDamage,
                    Delay = delay,
                    Target = c.Handle,
                    DamageType = DamageType.FullMagic
                });

                handle.RegisterResultHandler(result =>
                {
                    if (result.Damage.Succeeded)
                    {
                        OnHit(caster, c, result, delay);
                    }
                    else
                    {
                        OnMiss(c);
                    }
                });
            }
        }

        /// <summary>
        ///     Plays the casting animation.
        /// </summary>
        protected virtual void RenderCast(ICharacter caster) => caster.QueueAnimation(Animation.Create(SpellType is 1 or 3 ? 1979 : 1978));

        /// <summary>
        ///     Renders the caster graphic and the projectile, if the spell has them.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        /// <param name="duration">Projectile travel duration to the victim.</param>
        protected abstract void RenderCastEffects(ICharacter caster, ICreature victim, int duration);

        /// <summary>
        ///     Runs for every target right before its attack is queued.
        /// </summary>
        protected virtual void BeforeAttack(ICharacter caster, ICreature target, int damage, int delay)
        {
        }

        /// <summary>
        ///     Applies the family effect once a target's attack hit.
        /// </summary>
        protected abstract void OnHit(ICharacter caster, ICreature target, AttackResult result, int delay);

        /// <summary>
        ///     Shows the splash graphic once a target's attack missed.
        /// </summary>
        protected virtual void OnMiss(ICreature target) => target.QueueGraphic(Graphic.Create(85, height: 150));

        /// <summary>
        ///     Sends the standard Ancient spell projectile from the caster to the victim.
        /// </summary>
        protected void SendProjectile(ICharacter caster, ICreature victim, int graphicId, int duration, int? toHeight = null)
        {
            var projectile = ProjectileBuilder.Create()
                .WithGraphicId(graphicId)
                .FromCreature(caster)
                .ToCreature(victim)
                .WithDuration(duration)
                .WithFromHeight(43)
                .WithAngle(64)
                .WithSlope(16)
                .WithDelay(SpellDelay);
            if (toHeight is { } height)
            {
                projectile.WithToHeight(height);
            }

            projectile.Send();
        }

        /// <summary>
        ///     Determines whether this instance can attack the specified caster.
        /// </summary>
        public bool CanAttack(ICharacter caster, ICreature victim) => true;

        /// <summary>
        ///     Check's requirements for this spell.
        /// </summary>
        public virtual bool CheckRequirements(ICharacter caster) =>
            caster.Magic.CheckMagicLevel(LevelRequirements[SpellType]) && caster.Magic.CheckRunes(RuneRequirements[SpellType], RuneAmounts[SpellType]);

        /// <summary>
        ///     Removes required items from actor.
        /// </summary>
        public void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(RuneRequirements[SpellType], RuneAmounts[SpellType]);

        /// <summary>
        ///     Get's called when autocasting is set to this spell.
        /// </summary>
        public void OnAutoCastingActivation(ICharacter activatedOn) => activatedOn.Configurations.SendStandardConfiguration(108, ConfigIds[SpellType]);

        /// <summary>
        ///     Get's called when autocasting is unset to this spell.
        /// </summary>
        public void OnAutoCastingDeactivation(ICharacter deactivatedOn) => deactivatedOn.Configurations.SendStandardConfiguration(108, -1);

        /// <summary>
        ///     Get's speed of this spell.
        /// </summary>
        public int GetCastingSpeed(ICharacter caster) => 5;

        /// <summary>
        ///     Get's combat distance of this spell.
        /// </summary>
        public int GetCombatDistance(ICharacter caster) => 8;

        /// <summary>
        ///     Get's amount of magic experience this spell gives.
        /// </summary>
        public double GetMagicExperience() => Experience[SpellType];

        private static int GetTravelDuration(ICharacter caster, ICreature target) =>
            Math.Abs(caster.Location.X - target.Location.X) * 5 + Math.Abs(caster.Location.Y - target.Location.Y) * 5;
    }
}
