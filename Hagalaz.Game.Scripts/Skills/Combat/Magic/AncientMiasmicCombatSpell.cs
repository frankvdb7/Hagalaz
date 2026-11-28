using Hagalaz.Game.Abstractions.Builders.Projectile;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Features.States.Effects;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    ///     Class for all miasmic blitz,burst,barrage,rush
    /// </summary>
    public class AncientMiasmicCombatSpell : AncientCombatSpell
    {
        public AncientMiasmicCombatSpell(int spellType, CombatSpellDto dto, IProjectileBuilder projectileBuilder) : base(spellType, dto, projectileBuilder)
        {
        }

        /// <summary>
        ///     The STAF f_ IDS
        /// </summary>
        private static readonly int[] _staffIds = [13867, 13869, 13941, 13943];

        /// <summary>
        ///     Check's requirements for this spell. Zuriel's staff must be equipped.
        /// </summary>
        public override bool CheckRequirements(ICharacter caster)
        {
            var hasStaff = false;
            for (var i = 0; i < _staffIds.Length; i++)
            {
                if (caster.Equipment.Items.GetById((short)_staffIds[i]) != null)
                {
                    hasStaff = true;
                    break;
                }
            }

            if (!hasStaff)
            {
                caster.SendChatMessage("You must have Zuriel's staff equipped to cast this spell.");
                return false;
            }

            return base.CheckRequirements(caster);
        }

        private static readonly int[] _castAnimationIds = [10513, 10516, 10524, 10518];

        protected override void RenderCast(ICharacter caster) => caster.QueueAnimation(Animation.Create(_castAnimationIds[SpellType]));

        protected override void RenderCastEffects(ICharacter caster, ICreature victim, int duration)
        {
            switch (SpellType)
            {
                case 0:
                    caster.QueueGraphic(Graphic.Create(1846));
                    SendProjectile(caster, victim, 1846, duration, 22);
                    break;
                case 1: caster.QueueGraphic(Graphic.Create(1848)); break;
                case 2:
                    caster.QueueGraphic(Graphic.Create(1850));
                    SendProjectile(caster, victim, 1852, duration, 22);
                    break;
                case 3: caster.QueueGraphic(Graphic.Create(1853)); break;
            }
        }

        protected override void OnHit(ICharacter caster, ICreature target, AttackResult result, int delay)
        {
            switch (SpellType)
            {
                case 0: target.QueueGraphic(Graphic.Create(1847)); break;
                case 1: target.QueueGraphic(Graphic.Create(1849)); break;
                case 2: target.QueueGraphic(Graphic.Create(1851)); break;
                case 3: target.QueueGraphic(Graphic.Create(1854)); break;
            }

            if (target is ICharacter)
            {
                target.AddState(new MiasmicSlowState { TicksLeft = (SpellType + 1) * 12000 / 600 });
                target.AddState(new MiasmicSlowImmunityState { TicksLeft = (SpellType + 1) * 12000 / 600 });
            }
        }
    }
}
