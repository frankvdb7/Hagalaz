using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Class for all ice blitz,burst,barrage,rush
    /// </summary>
    public class AncientIceCombatSpell : AncientCombatSpell
    {
        public AncientIceCombatSpell(int spellType, CombatSpellDto dto, IProjectileBuilder projectileBuilder) : base(spellType, dto, projectileBuilder)
        {
        }

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
