using System;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// Defines the contract for a script that controls the behavior of an equippable item.
    /// </summary>
    public interface IEquipmentScript
    {
        /// <summary>
        /// Gets the attack speed of a specific item.
        /// </summary>
        /// <param name="item">The item instance.</param>
        /// <returns>The attack speed in game ticks.</returns>
        int GetAttackSpeed(IItem item);

        /// <summary>
        /// Gets the attack distance of a specific item.
        /// </summary>
        /// <param name="item">The item instance.</param>
        /// <returns>The attack distance in tiles.</returns>
        int GetAttackDistance(IItem item);

        /// <summary>
        /// Gets the attack style for a given combat option.
        /// </summary>
        /// <param name="item">The weapon item instance.</param>
        /// <param name="attackStyleOptionID">The ID of the selected combat option.</param>
        /// <returns>The <see cref="AttackStyle"/>.</returns>
        AttackStyle GetAttackStyle(IItem item, int attackStyleOptionID);

        /// <summary>
        /// Gets the attack bonus type for a given combat option.
        /// </summary>
        /// <param name="item">The weapon item instance.</param>
        /// <param name="attackStyleOptionID">The ID of the selected combat option.</param>
        /// <returns>The <see cref="AttackBonus"/> type.</returns>
        AttackBonus GetAttackBonusType(IItem item, int attackStyleOptionID);

        /// <summary>
        /// A callback executed when this item is equipped by a character.
        /// </summary>
        /// <param name="item">The item instance.</param>
        /// <param name="character">The character who equipped the item.</param>
        void OnEquipped(IItem item, ICharacter character);

        /// <summary>
        /// A callback executed when this item is unequipped by a character.
        /// </summary>
        /// <param name="item">The item instance.</param>
        /// <param name="character">The character who unequipped the item.</param>
        void OnUnequipped(IItem item, ICharacter character);

        /// <summary>
        /// A callback executed after a character dies while wearing this item.
        /// </summary>
        /// <param name="item">The item instance.</param>
        /// <param name="character">The character who died.</param>
        void OnDeath(IItem item, ICharacter character);

        /// <summary>
        /// Renders the weapon's attack animation and graphics.
        /// </summary>
        /// <param name="item">The weapon item instance.</param>
        /// <param name="animator">The character performing the attack.</param>
        /// <param name="specialAttack">A value indicating whether a special attack is being used.</param>
        void RenderAttack(IItem item, ICharacter animator, bool specialAttack);

        /// <summary>
        /// Performs the weapon's special attack against a victim.
        /// </summary>
        /// <param name="item">The weapon item instance.</param>
        /// <param name="attacker">The character performing the attack.</param>
        /// <param name="victim">The creature being targeted.</param>
        void PerformSpecialAttack(IItem item, ICharacter attacker, ICreature victim);

        /// <summary>
        /// Performs a standard (non-special) attack against a victim.
        /// </summary>
        /// <param name="item">The weapon item instance.</param>
        /// <param name="attacker">The character performing the attack.</param>
        /// <param name="victim">The creature being targeted.</param>
        void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim);

        /// <summary>
        /// A callback executed when an incoming attack is initiated against the wearer of this item.
        /// </summary>
        /// <param name="item">The item instance.</param>
        /// <param name="victim">The character being attacked.</param>
        /// <param name="attacker">The creature performing the attack.</param>
        /// <param name="damageType">The type of damage being dealt.</param>
        /// <param name="damage">The potential amount of damage, or -1 for a miss.</param>
        /// <param name="delay">The delay in client ticks until the attack connects.</param>
        /// <returns>The modified damage amount after any script-specific effects.</returns>
        int OnIncomingAttack(IItem item, ICharacter victim, ICreature attacker, DamageType damageType, int damage, int delay);

        /// <summary>
        /// A callback executed when an attack connects with the wearer of this item.
        /// </summary>
        /// <param name="item">The item instance.</param>
        /// <param name="victim">The character being attacked.</param>
        /// <param name="attacker">The creature performing the attack.</param>
        /// <param name="damageType">The type of damage dealt.</param>
        /// <param name="damage">A reference to the damage amount, which can be modified by the script.</param>
        void OnAttack(IItem item, ICharacter victim, ICreature attacker, DamageType damageType, ref int damage);

        /// <summary>
        /// A callback executed after the wearer of this item performs an attack.
        /// </summary>
        /// <param name="item">The item instance.</param>
        /// <param name="attacker">The character who performed the attack.</param>
        /// <param name="target">The creature that was attacked.</param>
        void OnAttackPerformed(IItem item, ICharacter attacker, ICreature target);

        /// <summary>
        /// Gets the amount of special attack energy required by this weapon.
        /// </summary>
        /// <param name="item">The weapon item instance.</param>
        /// <param name="attacker">The character performing the attack.</param>
        /// <returns>The required special energy amount.</returns>
        int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker);

        /// <summary>
        /// A callback executed when a character clicks the special attack bar while this weapon is equipped.
        /// </summary>
        /// <param name="item">The weapon item instance.</param>
        /// <param name="character">The character who clicked the bar.</param>
        /// <returns><c>true</c> if the special attack should be enabled; otherwise, <c>false</c>.</returns>
        bool SpecialBarEnableClicked(IItem item, ICharacter character);

        /// <summary>
        /// Renders the item's defence animation and graphics.
        /// </summary>
        /// <param name="item">The item instance.</param>
        /// <param name="animator">The character performing the defence.</param>
        /// <param name="delay">The delay in client ticks until the incoming attack connects.</param>
        void RenderDefence(IItem item, ICharacter animator, int delay);

        /// <summary>
        /// Checks if a character can unequip this item.
        /// </summary>
        /// <param name="item">The item to be unequipped.</param>
        /// <param name="character">The character attempting to unequip the item.</param>
        /// <returns><c>true</c> if the item can be unequipped; otherwise, <c>false</c>.</returns>
        bool CanUnEquipItem(IItem item, ICharacter character);

        /// <summary>
        /// Handles the logic for unequipping this item.
        /// </summary>
        /// <param name="item">The item to be unequipped.</param>
        /// <param name="character">The character unequipping the item.</param>
        /// <param name="toInventorySlot">The optional inventory slot to move the item to.</param>
        /// <returns><c>true</c> if the unequip was successful; otherwise, <c>false</c>.</returns>
        bool UnEquipItem(IItem item, ICharacter character, int toInventorySlot = -1);

        /// <summary>
        /// Handles the logic for equipping this item.
        /// </summary>
        /// <param name="item">The item to be equipped.</param>
        /// <param name="character">The character equipping the item.</param>
        /// <returns><c>true</c> if the equip was successful; otherwise, <c>false</c>.</returns>
        bool EquipItem(IItem item, ICharacter character);

        /// <summary>
        /// Checks if a character can equip this item.
        /// </summary>
        /// <param name="item">The item to be equipped.</param>
        /// <param name="character">The character attempting to equip the item.</param>
        /// <returns><c>true</c> if the item can be equipped; otherwise, <c>false</c>.</returns>
        bool CanEquipItem(IItem item, ICharacter character);
    }
}