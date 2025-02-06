using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEquipmentDefinition
    {
        /// <summary>
        /// Contains if it is weapon, and have special attack
        /// </summary>
        bool SpecialWeapon { get; set; }
        /// <summary>
        /// Contains item defence animation.
        /// </summary>
        int DefenceAnimation { get; set; }
        /// <summary>
        /// Contains item attack speed.
        /// </summary>
        int AttackSpeed { get; set; }
        /// <summary>
        /// Contains item attack distance.
        /// </summary>
        int AttackDistance { get; set; }
        /// <summary>
        /// Contains definition equipment type.
        /// </summary>
        EquipmentType Type { get; set; }
        /// <summary>
        /// Contains definition equipment slot.
        /// </summary>
        EquipmentSlot Slot { get; set; }
        /// <summary>
        /// Contains the requirements.
        /// </summary>
        IReadOnlyDictionary<int, int> Requirements { get; set; }
        /// <summary>
        /// Contains equipment bonuses.
        /// </summary>
        IBonuses Bonuses { get; set; }
        /// <summary>
        /// Contains attack bonuses ids.
        /// </summary>
        AttackBonus[] AttackBonusesIDs { get; set; }
        /// <summary>
        /// Contains attack style ids.
        /// </summary>
        AttackStyle[] AttackStyleIDs { get; set; }
        /// <summary>
        /// Get's attack animation of this definition.
        /// </summary>
        /// <param name="optionID">The option Id.</param>
        /// <returns>System.Int16.</returns>
        int GetAttackAnimationId(int optionID);
        /// <summary>
        /// Get's attack graphic of this definition.
        /// </summary>
        /// <param name="optionID">The option Id.</param>
        /// <returns>System.Int16.</returns>
        int GetAttackGraphicId(int optionID);
        /// <summary>
        /// Get's attack style of this definition.
        /// </summary>
        /// <param name="optionID">The option Id.</param>
        /// <returns>AttackStyle.</returns>
        AttackStyle GetAttackStyle(int optionID);
        /// <summary>
        /// Get's attack bonus of this definition.
        /// </summary>
        /// <param name="optionID">The option Id.</param>
        /// <returns>AttackBonus.</returns>
        AttackBonus GetAttackBonus(int optionID);
    }
}
