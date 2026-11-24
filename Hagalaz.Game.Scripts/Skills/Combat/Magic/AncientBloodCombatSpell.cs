using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Class for all blood blitz,burst,barrage,rush
    /// </summary>
    public class AncientBloodCombatSpell : AncientCombatSpell
    {
        public AncientBloodCombatSpell(int spellType, CombatSpellDto dto, IProjectileBuilder projectileBuilder) : base(spellType, dto, projectileBuilder)
        {
        }

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
