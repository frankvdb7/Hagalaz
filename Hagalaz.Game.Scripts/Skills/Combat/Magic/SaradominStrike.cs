using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    /// </summary>
    public class SaradominStrike : StandardCombatSpell
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SaradominStrike" /> class.
        /// </summary>
        /// <param name="dto">The definition.</param>
        public SaradominStrike(CombatSpellDto dto)
            : base(dto)
        {
        }

        /// <summary>
        ///     Called when [succesfull attack].
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <param name="victim">The victim.</param>
        public override void OnSuccessfulHit(ICharacter caster, ICreature victim)
        {
            if (victim is ICharacter character)
            {
                character.Statistics.DamageSkill(StatisticsConstants.Prayer, 10);
            }
        }

        /// <summary>
        ///     Check's requirements for this spell.
        /// </summary>
        /// <param name="caster"></param>
        /// <returns></returns>
        public override bool CheckRequirements(ICharacter caster)
        {
            var weapon = caster.Equipment[EquipmentSlot.Weapon];
            if (weapon == null || weapon.Id != (short)StaffType.SaradominStaff)
            {
                caster.SendChatMessage("You need to wear a Saradomin staff to cast this spell.");
                return false;
            }

            return base.CheckRequirements(caster);
        }

        /// <summary>
        ///     Gets the base damage.
        /// </summary>
        /// <param name="caster">The caster.</param>
        /// <returns></returns>
        public override int GetBaseDamage(ICharacter caster)
        {
            var cape = caster.Equipment[EquipmentSlot.Cape];
            if (cape != null && cape.Id == 2412 && caster.HasState(StateType.Charge))
            {
                return base.GetBaseDamage(caster) + 100;
            }

            return base.GetBaseDamage(caster);
        }
    }
}