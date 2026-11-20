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
    public class AncientBloodCombatSpell : AncientCombatSpell
    {
        public AncientBloodCombatSpell(int spellType, IProjectileBuilder projectileBuilder) : base(spellType, projectileBuilder)
        {
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

        protected override AncientSpellData Data { get; } = new(_runeRequirements, _runeAmounts, _levelRequirements, _configIds, _exp, _baseDamage);

        protected override bool SkullsAdditionalTargets => false;

        protected override void RenderCastEffects(ICharacter caster, ICreature victim, int duration)
        {
            if (SpellType == 2)
            {
                SendProjectile(caster, victim, 374, duration);
            }
        }

        protected override void BeforeAttack(ICharacter caster, ICreature target, int damage, int delay)
        {
            if (damage == -1)
            {
                target.QueueGraphic(Graphic.Create(85, delay, 150));
            }
            else
            {
                switch (SpellType)
                {
                    case 0: target.QueueGraphic(Graphic.Create(373, delay)); break;
                    case 1: target.QueueGraphic(Graphic.Create(376, delay)); break;
                    case 2: target.QueueGraphic(Graphic.Create(375, delay)); break;
                    case 3: target.QueueGraphic(Graphic.Create(377, delay)); break;
                }
            }
        }

        protected override void OnHit(ICharacter caster, ICreature target, AttackResult result, int delay)
        {
            var frozen = target.Freeze((SpellType + 1) * 9, (SpellType + 1) * 9 + 7);
            if (SpellType == 3 && frozen)
            {
                target.QueueGraphic(Graphic.Create(378, delay));
            }

            caster.Statistics.HealLifePoints((int)(result.Damage.Count * 0.25));
        }

        protected override void OnMiss(ICreature target)
        {
        }
    }
}
