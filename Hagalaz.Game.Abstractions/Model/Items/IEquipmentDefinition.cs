using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// Defines the contract for an item's equipment-specific properties, such as stats, requirements, and combat animations.
    /// </summary>
    public interface IEquipmentDefinition
    {
        /// <summary>
        /// Gets or sets a value indicating whether this item is a weapon with a special attack.
        /// </summary>
        bool SpecialWeapon { get; set; }

        /// <summary>
        /// Gets or sets the ID of the animation played when defending while this item is equipped.
        /// </summary>
        int DefenceAnimation { get; set; }

        /// <summary>
        /// Gets or sets the attack speed of this weapon in game ticks.
        /// </summary>
        int AttackSpeed { get; set; }

        /// <summary>
        /// Gets or sets the attack distance of this weapon in tiles.
        /// </summary>
        int AttackDistance { get; set; }

        /// <summary>
        /// Gets or sets the equipment type of this item (e.g., Normal, TwoHanded, FullBody).
        /// </summary>
        EquipmentType Type { get; set; }

        /// <summary>
        /// Gets or sets the equipment slot this item occupies.
        /// </summary>
        EquipmentSlot Slot { get; set; }

        /// <summary>
        /// Gets or sets a dictionary of skill level requirements to wield this item.
        /// </summary>
        IReadOnlyDictionary<int, int> Requirements { get; set; }

        /// <summary>
        /// Gets or sets the collection of combat bonuses provided by this item.
        /// </summary>
        IBonuses Bonuses { get; set; }

        /// <summary>
        /// Gets or sets the array of attack bonus types available with this weapon.
        /// </summary>
        AttackBonus[] AttackBonusesIDs { get; set; }

        /// <summary>
        /// Gets or sets the array of attack styles available with this weapon.
        /// </summary>
        AttackStyle[] AttackStyleIDs { get; set; }

        /// <summary>
        /// Gets the attack animation ID for a specific combat option.
        /// </summary>
        /// <param name="optionID">The ID of the selected combat option.</param>
        /// <returns>The attack animation ID.</returns>
        int GetAttackAnimationId(int optionID);

        /// <summary>
        /// Gets the attack graphic ID for a specific combat option.
        /// </summary>
        /// <param name="optionID">The ID of the selected combat option.</param>
        /// <returns>The attack graphic ID.</returns>
        int GetAttackGraphicId(int optionID);

        /// <summary>
        /// Gets the attack style for a specific combat option.
        /// </summary>
        /// <param name="optionID">The ID of the selected combat option.</param>
        /// <returns>The <see cref="AttackStyle"/>.</returns>
        AttackStyle GetAttackStyle(int optionID);

        /// <summary>
        /// Gets the attack bonus type for a specific combat option.
        /// </summary>
        /// <param name="optionID">The ID of the selected combat option.</param>
        /// <returns>The <see cref="AttackBonus"/> type.</returns>
        AttackBonus GetAttackBonus(int optionID);
    }
}