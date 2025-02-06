using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Services.GameWorld.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Data.Model
{
    /// <summary>
    /// Represents a equipable item's definition.
    /// </summary>
    public class EquipmentDefinition : IEquipmentDefinition
    {
        /// <summary>
        /// Contains definition id.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Contains definition equipment slot.
        /// </summary>
        public EquipmentSlot Slot { get; set; }

        /// <summary>
        /// Contains definition equipment type.
        /// </summary>
        public EquipmentType Type { get; set; }

        /// <summary>
        /// Contains if it is weapon , and have special attack
        /// </summary>
        public bool SpecialWeapon { get; set; }

        /// <summary>
        /// Contains item defence animation.
        /// </summary>
        public int DefenceAnimation { get; set; }

        /// <summary>
        /// Contains item attack speed.
        /// </summary>
        public int AttackSpeed { get; set; }

        /// <summary>
        /// Contains item attack distance.
        /// </summary>
        public int AttackDistance { get; set; }

        /// <summary>
        /// Contains the requirements.
        /// </summary>
        public IReadOnlyDictionary<int, int> Requirements { get; set; }

        /// <summary>
        /// Contains attack animations.
        /// </summary>
        public int[] AttackAnimations { get; set; }

        /// <summary>
        /// Contains attack graphics.
        /// </summary>
        public int[] AttackGraphics { get; set; }

        /// <summary>
        /// Contains attack bonuses ids.
        /// </summary>
        public AttackBonus[] AttackBonusesIDs { get; set; }

        /// <summary>
        /// Contains attack style ids.
        /// </summary>
        public AttackStyle[] AttackStyleIDs { get; set; }

        /// <summary>
        /// Contains equipment bonuses.
        /// </summary>
        public IBonuses Bonuses { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EquipmentDefinition"/> class.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        public EquipmentDefinition(int id)
        {
            Id = id;
            Slot = EquipmentSlot.NoSlot;
            Type = EquipmentType.Normal;
            SpecialWeapon = false;
            DefenceAnimation = -1;
            AttackSpeed = 8;
            AttackDistance = 1;
            Requirements = new Dictionary<int, int>();
            AttackBonusesIDs = new AttackBonus[4];
            AttackStyleIDs = new AttackStyle[4];
            AttackAnimations =
            [
                -1, -1, -1, -1
            ];
            AttackGraphics =
            [
                -1, -1, -1, -1
            ];
            Bonuses = new Bonuses();
        }

        /// <summary>
        /// Get's attack animation of this definition.
        /// </summary>
        /// <param name="optionID">The option Id.</param>
        /// <returns>System.Int16.</returns>
        public int GetAttackAnimationId(int optionID)
        {
            if (optionID < 0 || optionID >= 4)
                return -1;
            return AttackAnimations[optionID];
        }

        /// <summary>
        /// Get's attack graphic of this definition.
        /// </summary>
        /// <param name="optionID">The option Id.</param>
        /// <returns>System.Int16.</returns>
        public int GetAttackGraphicId(int optionID)
        {
            if (optionID < 0 || optionID >= 4)
                return -1;
            return AttackGraphics[optionID];
        }

        /// <summary>
        /// Get's attack bonus of this definition.
        /// </summary>
        /// <param name="optionID">The option Id.</param>
        /// <returns>AttackBonus.</returns>
        public AttackBonus GetAttackBonus(int optionID)
        {
            if (optionID < 0 || optionID >= 4)
                return AttackBonus.None;
            return AttackBonusesIDs[optionID];
        }

        /// <summary>
        /// Get's attack style of this definition.
        /// </summary>
        /// <param name="optionID">The option Id.</param>
        /// <returns>AttackStyle.</returns>
        public AttackStyle GetAttackStyle(int optionID)
        {
            if (optionID < 0 || optionID >= 4)
                return AttackStyle.None;
            return AttackStyleIDs[optionID];
        }

        /// <summary>
        /// Determines whether the specified skill id has the requirement.
        /// </summary>
        /// <param name="skillId">The skill id.</param>
        /// <param name="level">The level.</param>
        /// <returns><c>true</c> if the specified skill id has requirement; otherwise, <c>false</c>.</returns>
        public bool HasLevelRequirement(int skillId, int level)
        {
            if (!Requirements.ContainsKey(skillId))
                return true;
            return level >= Requirements[skillId];
        }

        /// <summary>
        /// Summerizes the definition.
        /// </summary>
        /// <returns>Returns the summerized version of this definition.</returns>
        public override string ToString() => "EquipmentDefinition[id=" + Id + "]";
    }
}