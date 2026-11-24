using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Common;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Class for all smoke blitz,burst,barrage,rush
    /// </summary>
    public class AncientSmokeCombatSpell : AncientCombatSpell
    {
        public AncientSmokeCombatSpell(int spellType, CombatSpellDto dto, IProjectileBuilder projectileBuilder) : base(spellType, dto, projectileBuilder)
        {
        }

        /// <summary>
        ///     The POSIO n_ DAMAGE
        /// </summary>
        private static readonly short[] _poisonDamage = [20, 20, 48, 40];

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
