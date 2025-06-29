using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Class for all blood blitz,burst,barrage,rush
    /// </summary>
    public class AncientBloodCombatSpell : ICombatSpell
    {
        /// <summary>
        ///     Construct's new ancient blood combat spell.
        /// </summary>
        public AncientBloodCombatSpell(int spellType, IProjectileBuilder projectileBuilder)
        {
            _spellType = spellType;
            _projectileBuilder = projectileBuilder;
        }

        /// <summary>
        ///     The RUN e_ REQUIREMENTS
        /// </summary>
        private static readonly RuneType[][] _runeRequirements =
        [
            [
                RuneType.Chaos, RuneType.Death, RuneType.Blood
            ],
            [
                RuneType.Chaos, RuneType.Death, RuneType.Blood
            ],
            [
                RuneType.Death, RuneType.Blood
            ],
            [
                RuneType.Death, RuneType.Blood, RuneType.Soul
            ]
        ];

        /// <summary>
        ///     The RUN e_ AMOUNTS
        /// </summary>
        private static readonly int[][] _runeAmounts =
        [
            [
                2, 2, 1
            ],
            [
                4, 2, 2
            ],
            [
                2, 4
            ],
            [
                4, 2, 1
            ]
        ];

        /// <summary>
        ///     The LEVE l_ REQUIREMENTS
        /// </summary>
        private static readonly int[] _levelRequirements =
        [
            56, 68, 80, 92
        ];

        /// <summary>
        ///     The CONFI g_ IDS
        /// </summary>
        private static readonly int[] _configIds =
        [
            67, 75, 83, 91
        ];

        /// <summary>
        ///     The EXP
        /// </summary>
        private static readonly double[] _exp =
        [
            33.0, 39.0, 45.0, 51.0
        ];

        /// <summary>
        ///     The BAS e_ DAMAGE
        /// </summary>
        private static readonly int[] _baseDamage =
        [
            150, 210, 250, 290
        ];

        private readonly int _spellType;
        private readonly IProjectileBuilder _projectileBuilder;

        /// <summary>
        ///     Perform's attack to target.
        /// </summary>
        public void PerformAttack(ICharacter caster, ICreature victim)
        {
            var multiHit = _spellType is 1 or 3;
            caster.QueueAnimation(Animation.Create(multiHit ? 1979 : 1978));
            var combat = (ICharacterCombat)caster.Combat;

            caster.Statistics.AddExperience(StatisticsConstants.Magic, _exp[_spellType]);

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


            if (_spellType == 2)
            {
                _projectileBuilder.Create()
                    .WithGraphicId(374)
                    .FromCreature(caster)
                    .ToCreature(victim)
                    .WithDuration(vDeltaX * 5 + vDeltaY * 5)
                    .WithFromHeight(43)
                    .WithDelay(51)
                    .WithSlope(16)
                    .WithAngle(64)
                    .Send();
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

                var maxDamage = combat.GetMagicMaxHit(c, _baseDamage[_spellType]);
                var damage = combat.GetMagicDamage(c, maxDamage);

                var deltaX = caster.Location.X - c.Location.X;
                var deltaY = caster.Location.Y - c.Location.Y;
                if (deltaX < 0)
                {
                    deltaX = -deltaX;
                }

                if (deltaY < 0)
                {
                    deltaY = -deltaY;
                }

                combat.PerformSoulSplit(c, damage);

                damage = c.Combat.IncomingAttack(caster, DamageType.FullMagic, damage, 51 + deltaX * 5 + deltaY * 5);
                combat.AddMagicExperience(damage);

                var delay = 51 + vDeltaX * 5 + vDeltaY * 5;

                caster.Statistics.HealLifePoints((int)(damage * 0.25));

                if (damage == -1)
                {
                    c.QueueGraphic(Graphic.Create(85, delay, 150));
                }
                else
                    switch (_spellType)
                    {
                        case 0: c.QueueGraphic(Graphic.Create(373, delay)); break;
                        case 1: c.QueueGraphic(Graphic.Create(376, delay)); break;
                        case 2: c.QueueGraphic(Graphic.Create(375, delay)); break;
                        case 3: c.QueueGraphic(Graphic.Create(377, delay)); break;
                    }

                caster.Combat.PerformAttack(new AttackParams()
                {
                    Damage = damage,
                    MaxDamage = maxDamage,
                    Target = c,
                    Delay = delay,
                    DamageType = DamageType.FullMagic
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
        public bool CheckRequirements(ICharacter caster) =>
            caster.Magic.CheckMagicLevel(_levelRequirements[_spellType]) && caster.Magic.CheckRunes(_runeRequirements[_spellType], _runeAmounts[_spellType]);

        /// <summary>
        ///     Removes required items from actor.
        /// </summary>
        /// <param name="caster"></param>
        public void RemoveRequirements(ICharacter caster) => caster.Magic.RemoveRunes(_runeRequirements[_spellType], _runeAmounts[_spellType]);

        /// <summary>
        ///     Get's called when autocasting is set to this spell.
        /// </summary>
        /// <param name="activatedOn"></param>
        public void OnAutoCastingActivation(ICharacter activatedOn) => activatedOn.Configurations.SendStandardConfiguration(108, _configIds[_spellType]);

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
        public double GetMagicExperience() => _exp[_spellType];
    }
}