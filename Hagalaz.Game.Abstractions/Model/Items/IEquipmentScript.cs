using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Model.Items
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEquipmentScript
    {
        /// <summary>
        /// Get's items for which this script is made.
        /// </summary>
        /// <returns>Return's array of item ids for which this script is suitable.</returns>
        [Obsolete("Use an EquipmentScriptFactory or EquipmentScriptMetaData instead")]
        IEnumerable<int> GetSuitableItems();
        /// <summary>
        /// Get's attack speed of specific item.
        /// By default , this method does return AttackSpeed field in EquipmentDefinition.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <returns>Attack speed of specific item.</returns>
        int GetAttackSpeed(IItem item);
        /// <summary>
        /// Get's attack distance of specific item.
        /// By default , this method does return AttackDistance field in EquipmentDefinition.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <returns>Attack speed of specific item.</returns>
        int GetAttackDistance(IItem item);
        /// <summary>
        /// Get's attack style.
        /// By default , this method does return GetAttackStyle(attackStyleOptionID) in EquipmentDefinition return'ed value.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attackStyleOptionID">Id of the attack style.</param>
        /// <returns>AttackStyle.</returns>
        AttackStyle GetAttackStyle(IItem item, int attackStyleOptionID);
        /// <summary>
        /// Get's attack bonus type.
        /// By default , this method does return GetAttackBonus(attackStyleOptionID) in EquipmentDefinition return'ed value.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attackStyleOptionID">The attack style option Id.</param>
        /// <returns>AttackBonus.</returns>
        AttackBonus GetAttackBonusType(IItem item, int attackStyleOptionID);
        /// <summary>
        /// Happens when this item is equiped by specific character.
        /// By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        void OnEquiped(IItem item, ICharacter character);
        /// <summary>
        /// Happens when this item is unequiped by specific character.
        /// By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        void OnUnequiped(IItem item, ICharacter character);
        /// <summary>
        /// Get's called after a character has died.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        void OnDeath(IItem item, ICharacter character);
        /// <summary>
        /// Render's weapon attack.
        /// This method is not guaranteed to be used when performing attacks.
        /// This method can throw NotImplementedException.
        /// </summary>
        /// <param name="item">Item in equipment instance.</param>
        /// <param name="animator">Character which is performing attack.</param>
        /// <param name="specialAttack">Wheter attack is special.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void RenderAttack(IItem item, ICharacter animator, bool specialAttack);
        /// <summary>
        /// Perform's special attack to victim.
        /// By default , this method does throw NotImplementedException
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim creature.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void PerformSpecialAttack(IItem item, ICharacter attacker, ICreature victim);
        /// <summary>
        /// Perform's standart ( Non special ) attack to victim.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim character.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim);

        /// <summary>
        /// Happens when incomming attack is performed to victim from attacker.
        /// By default this method does nothing and return's damage in parameters.
        /// </summary>
        /// <param name="item">Item in equipment instance.</param>
        /// <param name="victim">Character which is being attacked.</param>
        /// <param name="attacker">Creture which is attacking victim.</param>
        /// <param name="damageType">Type of the damage that is being received.</param>
        /// <param name="damage">Amount of damage, can be -1 incase of miss.</param>
        /// <param name="delay">Delay in client ticks until the attack will reach the target.</param>
        /// <returns>System.Int32.</returns>
        int OnIncomingAttack(IItem item, ICharacter victim, ICreature attacker, DamageType damageType, int damage, int delay);
        /// <summary>
        /// Get's called when character receives damage.
        /// By default this method returns damage amount in parameter.
        /// </summary>
        /// <param name="item">Item (shield or weapon) instance.</param>
        /// <param name="victim">Character which is defending.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="damageType">Type of the damage.</param>
        /// <param name="damage">Amount of damage, can be -1 incase of miss.</param>
        /// <returns>Amount of damage remains after defence.</returns>
        void OnAttack(IItem item, ICharacter victim, ICreature attacker, DamageType damageType, ref int damage);
        /// <summary>
        /// Get's called after a character performed an attack.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="attacker">The attacker.</param>
        /// <param name="target">The target.</param>
        void OnAttackPerformed(IItem item, ICharacter attacker, ICreature target);
        /// <summary>
        /// Get's amount of special energy required by this weapon.
        /// By default , this method does throw NotImplementedException
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <returns>System.Int16.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker);
        /// <summary>
        /// Happens when specific character clicks special bar ( which was not enabled ) on combat tab.
        /// By default this method does return true if weapon is special weapon.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="character">The character.</param>
        /// <returns>If the bar should be enabled.</returns>
        bool SpecialBarEnableClicked(IItem item, ICharacter character);

        /// <summary>
        /// Render's item defence animation and or graphics.
        /// Can throw NotImplementedException
        /// </summary>
        /// <param name="item">Item in animator's inventory.</param>
        /// <param name="animator">Character which is performing defence.</param>
        /// <param name="delay">Delay in client ticks before the attack will reach animator.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void RenderDefence(IItem item, ICharacter animator, int delay);
        /// <summary>
        /// Get's if specific character can unequip specific item.
        /// </summary>
        /// <param name="item">Item in character equipment.</param>
        /// <param name="character">Character which wants to unequip item.</param>
        /// <returns><c>true</c> if this instance [can un equip item] the specified item; otherwise, <c>false</c>.</returns>
        bool CanUnEquipItem(IItem item, ICharacter character);
        /// <summary>
        /// Get's called when specific item is about to be unequiped.
        /// </summary>
        /// <param name="item">Item in character equipment.</param>
        /// <param name="character">Character which should unequip the item.</param>
        /// <param name="toInventorySlot">To inventory slot.</param>
        /// <returns><c>true</c> if unequiped, <c>false</c> otherwise</returns>
        bool UnEquipItem(IItem item, ICharacter character, int toInventorySlot = -1);
        /// <summary>
        /// Get's called when specific item is about to be equiped.
        /// </summary>
        /// <param name="item">Item in character's inventory which should be equiped.</param>
        /// <param name="character">Character which should equip the item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool EquipItem(IItem item, ICharacter character);
        /// <summary>
        /// Get's if specific character can equip specific item.
        /// </summary>
        /// <param name="item">Item in character inventory.</param>
        /// <param name="character">Character which wants to equip item.</param>
        /// <returns><c>true</c> if this instance [can equip item] the specified item; otherwise, <c>false</c>.</returns>
        bool CanEquipItem(IItem item, ICharacter character);
    }
}
