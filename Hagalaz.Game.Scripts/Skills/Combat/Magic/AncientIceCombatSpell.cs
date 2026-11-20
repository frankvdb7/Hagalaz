using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Class for all ice blitz,burst,barrage,rush
    /// </summary>
    public class AncientIceCombatSpell : AncientCombatSpell
    {
        public AncientIceCombatSpell(int spellType, IProjectileBuilder projectileBuilder) : base(spellType, projectileBuilder)
        {
        }

        /// <summary>
        ///     The RUN e_ REQUIREMENTS
        /// </summary>
        private static readonly RuneType[][] _runeRequirements =
        [
            [
                RuneType.Chaos, RuneType.Death, RuneType.Water
            ],
            [
                RuneType.Chaos, RuneType.Death, RuneType.Water
            ],
            [
                RuneType.Death, RuneType.Blood, RuneType.Water
            ],
            [
                RuneType.Death, RuneType.Blood, RuneType.Water
            ]
        ];

        /// <summary>
        ///     The RUN e_ AMOUNTS
        /// </summary>
        private static readonly int[][] _runeAmounts =
        [
            [
                2, 2, 2
            ],
            [
                4, 2, 4
            ],
            [
                2, 2, 3
            ],
            [
                4, 2, 6
            ]
        ];

        /// <summary>
        ///     The LEVE l_ REQUIREMENTS
        /// </summary>
        private static readonly int[] _levelRequirements =
        [
            58, 70, 82, 94
        ];

        /// <summary>
        ///     The CONFI g_ IDS
        /// </summary>
        private static readonly int[] _configIds =
        [
            69, 77, 85, 93
        ];

        /// <summary>
        ///     The EXP
        /// </summary>
        private static readonly double[] _exp =
        [
            34.0, 40.0, 46.0, 52.0
        ];

        /// <summary>
        ///     The BAS e_ DAMAGE
        /// </summary>
        private static readonly int[] _baseDamage =
        [
            180, 220, 260, 300
        ];

        protected override AncientSpellData Data { get; } = new(_runeRequirements, _runeAmounts, _levelRequirements, _configIds, _exp, _baseDamage);

        protected override void RenderCastEffects(ICharacter caster, ICreature victim, int duration)
        {
            switch (SpellType)
            {
                case 0: SendProjectile(caster, victim, 360, duration); break;
                case 1: SendProjectile(caster, victim, 366, duration); break;
                case 2: caster.QueueGraphic(Graphic.Create(366, 0, 124)); break;
                case 3: SendProjectile(caster, victim, 368, duration); break;
            }
        }

        protected override void OnHit(ICharacter caster, ICreature target, AttackResult result, int delay)
        {
            var frozen = target.Freeze((SpellType + 1) * 9, (SpellType + 1) * 9 + 7);
            switch (SpellType)
            {
                case 3 when frozen: target.QueueGraphic(Graphic.Create(369, delay)); break;
                case 3: target.QueueGraphic(Graphic.Create(1677, delay, 150)); break;
                case 1: target.QueueGraphic(Graphic.Create(363, delay)); break;
                case 2: target.QueueGraphic(Graphic.Create(367, delay)); break;
                case 0: target.QueueGraphic(Graphic.Create(361, delay)); break;
            }
        }
    }
}
