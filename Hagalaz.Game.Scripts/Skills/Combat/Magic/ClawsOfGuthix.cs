using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    /// </summary>
    public class ClawsOfGuthix : StandardCombatSpell
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ClawsOfGuthix" /> class.
        /// </summary>
        /// <param name="dto">The definition.</param>
        public ClawsOfGuthix(CombatSpellDto dto)
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
            if (victim is ICharacter)
            {
                var target = (ICharacter)victim;
                var lowest = target.Statistics.LevelForExperience(StatisticsConstants.Defence) % 90;
                var level = target.Statistics.GetSkillLevel(StatisticsConstants.Defence);
                if (level > lowest)
                {
                    target.Statistics.DamageSkill(StatisticsConstants.Defence, (byte)(level - lowest));
                }
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
            if (weapon == null || weapon.Id != (short)StaffType.GuthixStaff)
            {
                caster.SendChatMessage("You need to wear a Guthix staff to cast this spell.");
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
            if (cape != null && cape.Id == 2413 && caster.HasState<ChargeState>())
            {
                return base.GetBaseDamage(caster) + 100;
            }

            return base.GetBaseDamage(caster);
        }
    }
}