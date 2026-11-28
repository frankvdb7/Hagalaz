using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Class for all shadow blitz,burst,barrage,rush
    /// </summary>
    public class AncientShadowCombatSpell : AncientCombatSpell
    {
        public AncientShadowCombatSpell(int spellType, CombatSpellDto dto, IProjectileBuilder projectileBuilder) : base(spellType, dto, projectileBuilder)
        {
        }

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
