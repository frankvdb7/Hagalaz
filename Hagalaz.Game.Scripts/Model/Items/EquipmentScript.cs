using System;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events.Character;

namespace Hagalaz.Game.Scripts.Model.Items
{
    /// <summary>
    /// Abstract equipment item class.
    /// </summary>
    public abstract class EquipmentScript : IEquipmentScript
    {
        /// <summary>
        /// Get's attack speed of specific item.
        /// By default , this method does return AttackSpeed field in EquipmentDefinition.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <returns>Attack speed of specific item.</returns>
        public virtual int GetAttackSpeed(IItem item) => item.EquipmentDefinition.AttackSpeed;

        /// <summary>
        /// Get's attack distance of specific item.
        /// By default , this method does return AttackDistance field in EquipmentDefinition.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <returns>Attack speed of specific item.</returns>
        public virtual int GetAttackDistance(IItem item) => item.EquipmentDefinition.AttackDistance;

        /// <summary>
        /// Get's attack bonus type.
        /// By default , this method does return GetAttackBonus(attackStyleOptionID) in EquipmentDefinition return'ed value.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attackStyleOptionID">The attack style option Id.</param>
        /// <returns>AttackBonus.</returns>
        public virtual AttackBonus GetAttackBonusType(IItem item, int attackStyleOptionID) => item.EquipmentDefinition.GetAttackBonus(attackStyleOptionID);

        /// <summary>
        /// Get's attack style.
        /// By default , this method does return GetAttackStyle(attackStyleOptionID) in EquipmentDefinition return'ed value.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attackStyleOptionID">Id of the attack style.</param>
        /// <returns>AttackStyle.</returns>
        public virtual AttackStyle GetAttackStyle(IItem item, int attackStyleOptionID) => item.EquipmentDefinition.GetAttackStyle(attackStyleOptionID);

        /// <summary>
        /// Perform's standart ( Non special ) attack to victim.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim character.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual void PerformStandardAttack(IItem item, ICharacter attacker, ICreature victim)
        {
            var style = attacker.Combat.GetAttackStyle();
            switch (style)
            {
                case AttackStyle.MeleeAccurate:
                case AttackStyle.MeleeAggressive:
                case AttackStyle.MeleeControlled:
                case AttackStyle.MeleeDefensive:
                    {
                        RenderAttack(item, attacker, false); // might throw NotImplementedException

                        var damage = ((ICharacterCombat)attacker.Combat).GetMeleeDamage(victim, false);
                        var maxDamage = ((ICharacterCombat)attacker.Combat).GetMeleeMaxHit(victim, false);

                        attacker.Combat.PerformAttack(new AttackParams()
                        {
                            Target = victim, Damage = damage, MaxDamage = maxDamage, DamageType = DamageType.FullMelee
                        });
                        break;
                    }
                case AttackStyle.RangedAccurate:
                case AttackStyle.RangedRapid:
                case AttackStyle.RangedLongRange:
                case AttackStyle.MagicNormal:
                case AttackStyle.MagicDefensive:
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Perform's special attack to victim.
        /// By default , this method does throw NotImplementedException
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <param name="victim">Victim creature.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual void PerformSpecialAttack(IItem item, ICharacter attacker, ICreature victim) => throw new NotImplementedException();

        /// <summary>
        /// Render's weapon attack.
        /// This method is not guaranteed to be used when performing attacks.
        /// This method can throw NotImplementedException.
        /// </summary>
        /// <param name="item">Item in equipment instance.</param>
        /// <param name="animator">Character which is performing attack.</param>
        /// <param name="specialAttack">Wheter attack is special.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual void RenderAttack(IItem item, ICharacter animator, bool specialAttack)
        {
            if (specialAttack)
                throw new NotImplementedException();

            var animation = item.EquipmentDefinition.GetAttackAnimationId(animator.Profile.GetValue<int>(ProfileConstants.CombatSettingsAttackStyleOptionId));
            animator.QueueAnimation(Animation.Create(animation));
            var graphic = item.EquipmentDefinition.GetAttackGraphicId(animator.Profile.GetValue<int>(ProfileConstants.CombatSettingsAttackStyleOptionId));
            animator.QueueGraphic(Graphic.Create(graphic));
        }


        /// <summary>
        /// Render's item defence animation and or graphics.
        /// Can throw NotImplementedException
        /// </summary>
        /// <param name="item">Item in animator's inventory.</param>
        /// <param name="animator">Character which is performing defence.</param>
        /// <param name="delay">Delay in client ticks before the attack will reach animator.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual void RenderDefence(IItem item, ICharacter animator, int delay)
        {
            if (item.EquipmentDefinition.DefenceAnimation == -1)
                throw new NotImplementedException();
            animator.QueueAnimation(Animation.Create(item.EquipmentDefinition.DefenceAnimation, delay));
        }

        /// <summary>
        /// Get's amount of special energy required by this weapon.
        /// By default , this method does throw NotImplementedException
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="attacker">Attacker character.</param>
        /// <returns>System.Int16.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public virtual int GetRequiredSpecialEnergyAmount(IItem item, ICharacter attacker) => 0;

        /// <summary>
        /// Happens when specific character clicks special bar ( which was not enabled ) on combat tab.
        /// By default this method does return true if weapon is special weapon.
        /// </summary>
        /// <param name="item">Weapon item instance.</param>
        /// <param name="character">The character.</param>
        /// <returns>If the bar should be enabled.</returns>
        public virtual bool SpecialBarEnableClicked(IItem item, ICharacter character) => item.EquipmentDefinition.SpecialWeapon;

        /// <summary>
        /// Happens when this item is equiped by specific character.
        /// By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public virtual void OnEquiped(IItem item, ICharacter character) { }

        /// <summary>
        /// Happens when this item is unequiped by specific character.
        /// By default , this method does nothing.
        /// </summary>
        /// <param name="item">Item instance.</param>
        /// <param name="character">Character which equiped the item.</param>
        public virtual void OnUnequiped(IItem item, ICharacter character) { }

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
        public virtual int OnIncomingAttack(IItem item, ICharacter victim, ICreature attacker, DamageType damageType, int damage, int delay) => damage;

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
        public virtual void OnAttack(IItem item, ICharacter victim, ICreature attacker, DamageType damageType, ref int damage) {}

        /// <summary>
        /// Get's called after a character performed an attack.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="attacker">The attacker.</param>
        /// <param name="target">The target.</param>
        public virtual void OnAttackPerformed(IItem item, ICharacter attacker, ICreature target) { }

        /// <summary>
        /// Get's called after a character has died.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public virtual void OnDeath(IItem item, ICharacter character) { }

        /// <summary>
        /// Get's if specific character can equip specific item.
        /// </summary>
        /// <param name="item">Item in character inventory.</param>
        /// <param name="character">Character which wants to equip item.</param>
        /// <returns><c>true</c> if this instance [can equip item] the specified item; otherwise, <c>false</c>.</returns>
        public virtual bool CanEquipItem(IItem item, ICharacter character)
        {
            if (character.EventManager.SendEvent(new EquipAllowEvent(character, item)))
            {
                var allow = true;
                foreach (var skillId in item.EquipmentDefinition.Requirements.Keys)
                {
                    var level = item.EquipmentDefinition.Requirements[skillId];
                    if (character.Statistics.LevelForExperience(skillId) < level)
                    {
                        allow = false;
                        character.SendChatMessage("You need a " + StatisticsConstants.SkillNames[skillId] + " level of " + level + " to wear this item.");
                    }
                }

                return allow;
            }

            return false;
        }

        /// <summary>
        /// Get's called when specific item is about to be equiped.
        /// </summary>
        /// <param name="item">Item in character's inventory which should be equiped.</param>
        /// <param name="character">Character which should equip the item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public virtual bool EquipItem(IItem item, ICharacter character) => character.Equipment.EquipItem(item);

        /// <summary>
        /// Get's called when specific item is about to be unequiped.
        /// </summary>
        /// <param name="item">Item in character equipment.</param>
        /// <param name="character">Character which should unequip the item.</param>
        /// <param name="toInventorySlot">To inventory slot.</param>
        /// <returns><c>true</c> if unequiped, <c>false</c> otherwise</returns>
        public virtual bool UnEquipItem(IItem item, ICharacter character, int toInventorySlot = -1) => character.Equipment.UnEquipItem(item, toInventorySlot);

        /// <summary>
        /// Get's if specific character can unequip specific item.
        /// </summary>
        /// <param name="item">Item in character equipment.</param>
        /// <param name="character">Character which wants to unequip item.</param>
        /// <returns><c>true</c> if this instance [can un equip item] the specified item; otherwise, <c>false</c>.</returns>
        public virtual bool CanUnEquipItem(IItem item, ICharacter character) => character.EventManager.SendEvent(new UnEquipAllowEvent(character, item));
    }
}