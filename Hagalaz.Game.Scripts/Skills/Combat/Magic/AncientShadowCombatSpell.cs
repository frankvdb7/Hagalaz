using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Class for all shadow blitz,burst,barrage,rush
    /// </summary>
    public class AncientShadowCombatSpell : AncientCombatSpell
    {
        public AncientShadowCombatSpell(int spellType, IProjectileBuilder projectileBuilder) : base(spellType, projectileBuilder)
        {
        }

        /// <summary>
        ///     The RUN e_ REQUIREMENTS
        /// </summary>
        private static readonly RuneType[][] _runeRequirements =
        [
            [RuneType.Chaos, RuneType.Death, RuneType.Air, RuneType.Soul], [RuneType.Chaos, RuneType.Death, RuneType.Air, RuneType.Soul], [
                RuneType.Death, RuneType.Blood, RuneType.Air, RuneType.Soul
            ],
            [RuneType.Death, RuneType.Blood, RuneType.Air, RuneType.Soul]
        ];

        /// <summary>
        ///     The RUN e_ AMOUNTS
        /// </summary>
        private static readonly int[][] _runeAmounts = [[2, 2, 1, 1], [4, 2, 1, 2], [2, 2, 2, 2], [4, 2, 4, 3]];

        /// <summary>
        ///     The LEVE l_ REQUIREMENTS
        /// </summary>
        private static readonly int[] _levelRequirements = [52, 64, 76, 88];

        /// <summary>
        ///     The CONFI g_ IDS
        /// </summary>
        private static readonly int[] _configIds = [65, 73, 81, 89];

        /// <summary>
        ///     The EXP
        /// </summary>
        private static readonly double[] _exp = [31.0, 37.0, 43.0, 49.0];

        /// <summary>
        ///     The BAS e_ DAMAGE
        /// </summary>
        private static readonly int[] _baseDamage = [140, 180, 240, 280];

        protected override RuneType[][] RuneRequirements => _runeRequirements;

        protected override int[][] RuneAmounts => _runeAmounts;

        protected override int[] LevelRequirements => _levelRequirements;

        protected override int[] ConfigIds => _configIds;

        protected override double[] Experience => _exp;

        protected override int[] BaseDamage => _baseDamage;

        protected override void RenderCastEffects(ICharacter caster, ICreature victim, int duration)
        {
            switch (SpellType)
            {
                case 0: SendProjectile(caster, victim, 378, duration); break;
                case 2: SendProjectile(caster, victim, 380, duration); break;
            }
        }

        protected override void OnHit(ICharacter caster, ICreature target, AttackResult result, int delay)
        {
            switch (SpellType)
            {
                case 0: target.QueueGraphic(Graphic.Create(379)); break;
                case 1: target.QueueGraphic(Graphic.Create(382)); break;
                case 2: target.QueueGraphic(Graphic.Create(381)); break;
                case 3: target.QueueGraphic(Graphic.Create(383)); break;
            }

            if (target is not ICharacter charTarget)
            {
                return;
            }

            var percent = SpellType == 3 ? 0.85D : 0.90D;
            var lowest = (int)(charTarget.Statistics.LevelForExperience(StatisticsConstants.Attack) * percent);
            var level = charTarget.Statistics.GetSkillLevel(StatisticsConstants.Attack);
            if (level > lowest)
            {
                charTarget.Statistics.DamageSkill(StatisticsConstants.Attack, level - lowest);
            }
        }
    }
}
