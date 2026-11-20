using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Common;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Class for all smoke blitz,burst,barrage,rush
    /// </summary>
    public class AncientSmokeCombatSpell : AncientCombatSpell
    {
        public AncientSmokeCombatSpell(int spellType, IProjectileBuilder projectileBuilder) : base(spellType, projectileBuilder)
        {
        }

        /// <summary>
        ///     The RUN e_ REQUIREMENTS
        /// </summary>
        private static readonly RuneType[][] _runeRequirements =
        [
            [RuneType.Chaos, RuneType.Death, RuneType.Fire, RuneType.Air], [RuneType.Chaos, RuneType.Death, RuneType.Fire, RuneType.Air], [
                RuneType.Death, RuneType.Blood, RuneType.Fire, RuneType.Air
            ],
            [RuneType.Death, RuneType.Blood, RuneType.Fire, RuneType.Air]
        ];

        /// <summary>
        ///     The RUN e_ AMOUNTS
        /// </summary>
        private static readonly int[][] _runeAmounts = [[2, 2, 1, 1], [4, 2, 2, 2], [2, 2, 2, 2], [4, 2, 4, 4]];

        /// <summary>
        ///     The LEVE l_ REQUIREMENTS
        /// </summary>
        private static readonly int[] _levelRequirements = [50, 62, 74, 86];

        /// <summary>
        ///     The CONFI g_ IDS
        /// </summary>
        private static readonly int[] _configIds = [63, 71, 79, 87];

        /// <summary>
        ///     The EXP
        /// </summary>
        private static readonly double[] _exp = [30.0, 36.0, 42.0, 48.0];

        /// <summary>
        ///     The BAS e_ DAMAGE
        /// </summary>
        private static readonly int[] _baseDamage = [130, 170, 264, 270];

        /// <summary>
        ///     The POSIO n_ DAMAGE
        /// </summary>
        private static readonly short[] _poisonDamage = [20, 20, 48, 40];

        protected override AncientSpellData Data { get; } = new(_runeRequirements, _runeAmounts, _levelRequirements, _configIds, _exp, _baseDamage);

        private static readonly int[] _projectileIds = [384, 388, 386, 390];

        private static readonly int[] _hitGraphicIds = [385, 389, 387, 391];

        protected override void RenderCastEffects(ICharacter caster, ICreature victim, int duration) =>
            SendProjectile(caster, victim, _projectileIds[SpellType], duration, 31);

        protected override void BeforeAttack(ICharacter caster, ICreature target, int damage, int delay)
        {
            if (RandomStatic.Generator.NextDouble() > 0.65)
            {
                target.Poison(_poisonDamage[SpellType]);
            }
        }

        protected override void OnHit(ICharacter caster, ICreature target, AttackResult result, int delay) =>
            target.QueueGraphic(Graphic.Create(_hitGraphicIds[SpellType], height: 124));
    }
}
