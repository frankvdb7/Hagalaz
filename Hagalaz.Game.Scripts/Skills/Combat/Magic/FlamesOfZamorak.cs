using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Scripts.Skills.Combat.Magic
{
    /// <summary>
    /// </summary>
    public class FlamesOfZamorak : StandardCombatSpell
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FlamesOfZamorak" /> class.
        /// </summary>
        /// <param name="dto">The definition.</param>
        public FlamesOfZamorak(CombatSpellDto dto)
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
            if (victim is ICharacter target)
            {
                var lowest = target.Statistics.LevelForExperience(StatisticsConstants.Magic) - 5;
                var level = target.Statistics.GetSkillLevel(StatisticsConstants.Magic);
                if (level > lowest)
                {
                    target.Statistics.DamageSkill(StatisticsConstants.Magic, (byte)(level - lowest));
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
            if (weapon == null || weapon.Id != (short)StaffType.ZamorakStaff)
            {
                caster.SendChatMessage("You need to wear a Zamorak staff to cast this spell.");
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
            if (cape != null && cape.Id == 2414 && caster.HasState<ChargeState>())
            {
                return base.GetBaseDamage(caster) + 100;
            }

            return base.GetBaseDamage(caster);
        }
    }
}