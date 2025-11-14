using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Class for all miasmic blitz,burst,barrage,rush
    /// </summary>
    public class AncientMiasmicCombatSpell : ICombatSpell
    {
        private readonly IProjectileBuilder _projectileBuilder;

        /// <summary>
        ///     Contains type of the spell.
        /// </summary>
        public int SpellType { get; }

        public AncientMiasmicCombatSpell(int spellType, IProjectileBuilder projectileBuilder)
        {
            _projectileBuilder = projectileBuilder;
            SpellType = spellType;
        }

        /// <summary>
        ///     The RUN e_ REQUIREMENTS
        /// </summary>
        private static readonly RuneType[][] _runeRequirements =
        [
            [RuneType.Chaos, RuneType.Earth, RuneType.Soul], [RuneType.Chaos, RuneType.Earth, RuneType.Soul], [RuneType.Blood, RuneType.Earth, RuneType.Soul],
            [RuneType.Blood, RuneType.Earth, RuneType.Soul]
        ];

        /// <summary>
        ///     The RUN e_ AMOUNTS
        /// </summary>
        private static readonly int[][] _runeAmounts = [[2, 1, 1], [4, 2, 2], [2, 3, 3], [4, 4, 4]];

        /// <summary>
        ///     The LEVE l_ REQUIREMENTS
        /// </summary>
        private static readonly int[] _levelRequirements = [61, 73, 85, 97];

        /// <summary>
        ///     The CONFI g_ IDS
        /// </summary>
        private static readonly int[] _configIds = [95, 97, 99, 101];

        /// <summary>
        ///     The EXP
        /// </summary>
        private static readonly double[] _exp = [36.0, 42.0, 48.0, 54.0];

        /// <summary>
        ///     The BAS e_ DAMAGE
        /// </summary>
        private static int[] _baseDamage;

        static AncientMiasmicCombatSpell()
        {
            _baseDamage = new int[] { 180, 240, 280, 352 };
        }

        /// <summary>
        ///     The STAF f_ IDS
        /// </summary>
        private static readonly int[] _staffIds = [13867, 13869, 13941, 13943];

        /// <summary>
        ///     Perform's attack to target.
        /// </summary>
        public void PerformAttack(ICharacter caster, ICreature victim)
        {
            var multiHit = SpellType is 1 or 3;

            switch (SpellType)
            {
                case 0: caster.QueueAnimation(Animation.Create(10513)); break;
                case 1: caster.QueueAnimation(Animation.Create(10516)); break;
                case 2: caster.QueueAnimation(Animation.Create(10524)); break;
                case 3: caster.QueueAnimation(Animation.Create(10518)); break;
            }

            var combat = (ICharacterCombat)caster.Combat;

            caster.Statistics.AddExperience(StatisticsConstants.Magic, _exp[SpellType]);

            var vDeltaX = caster.Location.X - victim.Location.X;
            var vDeltaY = caster.Location.Y - victim.Location.Y;
            if (vDeltaX < 0)
            {
                vDeltaX = -vDeltaX;
            }

            if (vDeltaY < 0)
            {
                vDeltaY = -vDeltaY;
            }

            var duration = vDeltaX * 5 + vDeltaY * 5;

            switch (SpellType)
            {
                case 0:
                    {
                        caster.QueueGraphic(Graphic.Create(1846));
                        _projectileBuilder.Create()
                            .WithGraphicId(1846)
                            .FromCreature(caster)
                            .ToCreature(victim)
                            .WithDuration(duration)
                            .WithFromHeight(43)
                            .WithToHeight(22)
                            .WithAngle(64)
                            .WithSlope(16)
                            .WithDelay(51)
                            .Send();
                        break;
                    }
                case 1: caster.QueueGraphic(Graphic.Create(1848)); break;
                case 2:
                    {
                        caster.QueueGraphic(Graphic.Create(1850));
                        _projectileBuilder.Create()
                            .WithGraphicId(1852)
                            .FromCreature(caster)
                            .ToCreature(victim)
                            .WithDuration(duration)
                            .WithFromHeight(43)
                            .WithToHeight(22)
                            .WithAngle(64)
                            .WithSlope(16)
                            .WithDelay(51)
                            .Send();
                        break;
                    }
                case 3: caster.QueueGraphic(Graphic.Create(1853)); break;
            }

            var visibleCreatures = caster.Viewport.VisibleCreatures;
            foreach (var c in visibleCreatures)
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
                if (c != victim)
                {
                    combat.CheckSkullConditions(c);
                }

                var creature = c;
                var maxDamage = combat.GetMagicMaxHit(creature, _baseDamage[SpellType]);
                var damage = combat.GetMagicDamage(creature, maxDamage);

                var deltaX = caster.Location.X - creature.Location.X;
                var deltaY = caster.Location.Y - creature.Location.Y;
                if (deltaX < 0)
                {
                    deltaX = -deltaX;
                }

                if (deltaY < 0)
                {
                    deltaY = -deltaY;
                }

                var delay = 51 + deltaX * 5 + deltaY * 5;

                var handle = caster.Combat.PerformAttack(new AttackParams()
                {
                    Damage = damage,
                    MaxDamage = maxDamage,
                    Delay = delay,
                    DamageType = DamageType.FullMagic,
                    Target = creature,
                });

                handle.RegisterResultHandler(result =>
                {
                    if (result.Damage.Succeeded)
                    {
                        switch (SpellType)
                        {
                            case 0: creature.QueueGraphic(Graphic.Create(1847)); break;
                            case 1: creature.QueueGraphic(Graphic.Create(1849)); break;
                            case 2: creature.QueueGraphic(Graphic.Create(1851)); break;
                            case 3: creature.QueueGraphic(Graphic.Create(1854)); break;
                        }
                        if (c is ICharacter)
                        {
                            c.AddState(new MiasmicSlowState { TicksLeft = (SpellType + 1) * 12000 / 600 });
                            c.AddState(new MiasmicSlowImmunityState { TicksLeft = (SpellType + 1) * 12000 / 600 });
                        }
                    }
                    else
                    {
                        creature.QueueGraphic(Graphic.Create(85, height: 150));
                    }
                });
            }
        }

        /// <summary>
        ///     Determines whether this instance can attack the specified caster.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        /// <returns></returns>
        public bool CanAttack(ICharacter caster, ICreature victim) => true;

        /// <summary>
        ///     Check's requirements for this spell.
        /// </summary>
        /// <returns></returns>
        public bool CheckRequirements(ICharacter caster)
        {
            var hasStaff = false;
            for (var i = 0; i < _staffIds.Length; i++)
            {
                if (caster.Equipment.GetById((short)_staffIds[i]) != null)
                {
                    hasStaff = true;
                    break;
                }
            }

            if (!hasStaff)
            {
                caster.SendChatMessage("You must have Zuriel's staff equiped to cast this spell.");
                return false;
            }

            return caster.Magic.CheckMagicLevel(_levelRequirements[SpellType]) &&
                   caster.Magic.CheckRunes(_runeRequirements[SpellType], _runeAmounts[SpellType]);
        }

        /// <summary>
        ///     Removes required items from actor.
        /// </summary>
        /// <param name="caster"></param>
        public void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(_runeRequirements[SpellType], _runeAmounts[SpellType]);

        /// <summary>
        ///     Get's called when autocasting is set to this spell.
        /// </summary>
        /// <param name="activatedOn"></param>
        public void OnAutoCastingActivation(ICharacter activatedOn) => activatedOn.Configurations.SendStandardConfiguration(108, _configIds[SpellType]);

        /// <summary>
        ///     Get's called when autocasting is unset to this spell.
        /// </summary>
        /// <param name="deactivatedOn"></param>
        public void OnAutoCastingDeactivation(ICharacter deactivatedOn) => deactivatedOn.Configurations.SendStandardConfiguration(108, -1);

        /// <summary>
        ///     Get's speed of this spell.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public int GetCastingSpeed(ICharacter caster) => 5;

        /// <summary>
        ///     Get's combat distance of this spell.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public int GetCombatDistance(ICharacter caster) => 8;

        /// <summary>
        ///     Get's amount of magic experience this spell gives.
        /// </summary>
        /// <returns></returns>
        public double GetMagicExperience() => _exp[SpellType];
    }
}
