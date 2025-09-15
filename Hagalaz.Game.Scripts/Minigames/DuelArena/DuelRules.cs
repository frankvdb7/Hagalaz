using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events.Character;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena
{
    /// <summary>
    /// </summary>
    public class DuelRules
    {
        /// <summary>
        /// </summary>
        public enum Rule : byte
        {
            /// <summary>
            ///     The no ranged
            /// </summary>
            NoRanged = 0,

            /// <summary>
            ///     The no melee
            /// </summary>
            NoMelee = 1,

            /// <summary>
            ///     The no magic
            /// </summary>
            NoMagic = 2,

            /// <summary>
            ///     The no drinks
            /// </summary>
            NoDrinks = 3,

            /// <summary>
            ///     The no food
            /// </summary>
            NoFood = 4,

            /// <summary>
            ///     The no prayer
            /// </summary>
            NoPrayer = 5,

            /// <summary>
            ///     The obstacles
            /// </summary>
            Obstacles = 6,

            /// <summary>
            ///     The no forfeit
            /// </summary>
            NoForfeit = 7,

            /// <summary>
            ///     The fun weapons
            /// </summary>
            FunWeapons = 8,

            /// <summary>
            ///     The no special attacks
            /// </summary>
            NoSpecialAttacks = 9,

            /// <summary>
            ///     The no helm
            /// </summary>
            NoHelm = 10,

            /// <summary>
            ///     The no cape
            /// </summary>
            NoCape = 11,

            /// <summary>
            ///     The no amulet
            /// </summary>
            NoAmulet = 12,

            /// <summary>
            ///     The no weapon
            /// </summary>
            NoWeapon = 13,

            /// <summary>
            ///     The no body
            /// </summary>
            NoBody = 14,

            /// <summary>
            ///     The no shield
            /// </summary>
            NoShield = 15,

            /// <summary>
            ///     The no legs
            /// </summary>
            NoLegs = 17,

            /// <summary>
            ///     The no gloves
            /// </summary>
            NoGloves = 19,

            /// <summary>
            ///     The no boots
            /// </summary>
            NoBoots = 20,

            /// <summary>
            ///     The no ring
            /// </summary>
            NoRing = 22,

            /// <summary>
            ///     The no ammo
            /// </summary>
            NoAmmo = 23,

            /// <summary>
            ///     The enable summoning
            /// </summary>
            EnableSummoning = 24,

            /// <summary>
            ///     The no movement
            /// </summary>
            NoMovement = 25
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="DuelRules" /> class.
        /// </summary>
        public DuelRules(Action<Rule> ruleChangedCallback) => _ruleChangedCallback = ruleChangedCallback;

        /// <summary>
        ///     The rules.
        /// </summary>
        private bool[] _rules = new bool[26];

        /// <summary>
        ///     The rules changed callback
        /// </summary>
        private readonly Action<Rule> _ruleChangedCallback;

        /// <summary>
        ///     Gets the <see cref="System.Boolean" /> at the specified rule id.
        /// </summary>
        /// <value>
        ///     The <see cref="System.Boolean" />.
        /// </value>
        /// <param name="rule">The rule.</param>
        /// <returns></returns>
        public bool this[Rule rule]
        {
            get => _rules[(byte)rule];
            private set => _rules[(byte)rule] = value;
        }

        /// <summary>
        ///     Enables the rule.
        /// </summary>
        /// <param name="rule">The rule.</param>
        public void ToggleRule(Rule rule)
        {
            this[rule] = !this[rule];
            if (_ruleChangedCallback != null)
            {
                _ruleChangedCallback(rule);
            }
        }

        /// <summary>
        ///     Checks the rules.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="other">The other.</param>
        /// <returns></returns>
        public bool CheckRules(ICharacter character, ICharacter other)
        {
            if (this[Rule.NoRanged] && this[Rule.NoMelee] && this[Rule.NoMagic])
            {
                character.SendChatMessage("You need to be able to use at least one combat style in a duel.");
                other.SendChatMessage("You need to be able to use at least one combat style in a duel.");
                return false;
            }

            var equipment = new List<IItem>();
            for (var ruleId = 10; ruleId < 24; ruleId++)
            {
                if (this[(Rule)ruleId])
                {
                    var slot = ruleId - 10;
                    var item = character.Equipment[(EquipmentSlot)slot];
                    if (item != null)
                    {
                        equipment.Add(item);
                    }
                }
            }

            if (equipment.Count == 0)
            {
                return true;
            }

            if (character.Inventory.HasSpaceForRange(equipment.ToArray()))
            {
                return true;
            }

            character.SendChatMessage("You do not have enough inventory space to remove all the equipment.");
            other.SendChatMessage("Other player doesn't have enough space in their inventory to remove all the equipment.");
            return false;

        }

        /// <summary>
        ///     Removes the equipment.
        /// </summary>
        /// <param name="character">The character.</param>
        public void RemoveEquipment(ICharacter character)
        {
            for (var ruleId = 10; ruleId < 24; ruleId++)
            {
                if (!this[(Rule)ruleId])
                {
                    continue;
                }

                var slot = ruleId - 10;
                var item = character.Equipment[(EquipmentSlot)slot];
                if (item != null)
                {
                    character.Equipment.UnEquipItem(item);
                }
            }
        }

        /// <summary>
        ///     Sends the rules.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="target">The target.</param>
        public void SendRules(ICharacter character, ICharacter target)
        {
            var value = 0;
            var increment = 16;
            for (var i = 0; i < _rules.Length; i++)
            {
                if (_rules[i])
                {
                    if (i == 7)
                    {
                        value += 5;
                    }
                    else if (i == 25)
                    {
                        value += 6;
                    }

                    value += increment;
                }

                increment += increment;
            }

            character.Configurations.SendStandardConfiguration(286, value);
            target.Configurations.SendStandardConfiguration(286, value);
        }

        /// <summary>
        ///     Applies the rules.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="script">The script.</param>
        public void ApplyRules(ICharacter character, DuelArenaCombatScript script)
        {
            if (this[Rule.NoMovement])
            {
                EventHappened? happ = null;
                happ = character.RegisterEventHandler(new EventHappened<WalkAllowEvent>(e =>
                {
                    if (character.GetScript<DuelArenaCombatScript>() == script)
                    {
                        character.SendChatMessage("Movement has been disabled.");
                        return true;
                    }

                    character.UnregisterEventHandler<WalkAllowEvent>(happ!);
                    return false;
                }));
            }

            if (!this[Rule.EnableSummoning])
            {
                EventHappened? happ = null;
                happ = character.RegisterEventHandler(new EventHappened<SummoningAllowEvent>(e =>
                {
                    if (character.GetScript<DuelArenaCombatScript>() == script)
                    {
                        character.SendChatMessage("Summoning has been disabled.");
                        return true;
                    }

                    character.UnregisterEventHandler<SummoningAllowEvent>(happ!);
                    return false;
                }));
            }

            if (this[Rule.NoDrinks])
            {
                EventHappened? happ = null;
                happ = character.RegisterEventHandler(new EventHappened<DrinkAllowEvent>(e =>
                {
                    if (character.GetScript<DuelArenaCombatScript>() == script)
                    {
                        character.SendChatMessage("Drinking has been disabled.");
                        return true;
                    }

                    character.UnregisterEventHandler<DrinkAllowEvent>(happ!);
                    return false;
                }));
            }

            if (this[Rule.NoFood])
            {
                EventHappened? happ = null;
                happ = character.RegisterEventHandler(new EventHappened<EatAllowEvent>(e =>
                {
                    if (character.GetScript<DuelArenaCombatScript>() == script)
                    {
                        character.SendChatMessage("Food has been disabled.");
                        return true;
                    }

                    character.UnregisterEventHandler<EatAllowEvent>(happ!);
                    return false;
                }));
            }

            if (this[Rule.NoSpecialAttacks])
            {
                EventHappened? happ = null;
                happ = character.RegisterEventHandler(new EventHappened<SpecialAllowEvent>(e =>
                {
                    if (character.GetScript<DuelArenaCombatScript>() == script)
                    {
                        character.SendChatMessage("Special attacks have been disabled.");
                        return true;
                    }

                    character.UnregisterEventHandler<SpecialAllowEvent>(happ!);
                    return false;
                }));
            }

            if (this[Rule.NoPrayer])
            {
                EventHappened? happ = null;
                happ = character.RegisterEventHandler(new EventHappened<PrayerAllowEvent>(e =>
                {
                    if (character.GetScript<DuelArenaCombatScript>() == script)
                    {
                        character.SendChatMessage("Prayer have been disabled.");
                        return true;
                    }

                    character.UnregisterEventHandler<PrayerAllowEvent>(happ!);
                    return false;
                }));
            }

            if (this[Rule.NoMelee])
            {
                EventHappened? happ = null;
                happ = character.RegisterEventHandler(new EventHappened<AttackAllowEvent>(e =>
                {
                    if (character.GetScript<DuelArenaCombatScript>() == script)
                    {
                        if (e.Style == AttackStyle.MeleeAccurate || e.Style == AttackStyle.MeleeAggressive ||
                            e.Style == AttackStyle.MeleeControlled || e.Style == AttackStyle.MeleeDefensive)
                        {
                            {
                                character.SendChatMessage("Melee has been disabled.");
                                return true;
                            }
                        }
                    }

                    character.UnregisterEventHandler<AttackAllowEvent>(happ!);
                    return false;
                }));
            }

            if (this[Rule.NoRanged])
            {
                EventHappened? happ = null;
                happ = character.RegisterEventHandler(new EventHappened<AttackAllowEvent>(e =>
                {
                    if (character.GetScript<DuelArenaCombatScript>() == script)
                    {
                        if (e.Style == AttackStyle.RangedAccurate || e.Style == AttackStyle.RangedLongRange ||
                            e.Style == AttackStyle.RangedRapid)
                        {
                            {
                                character.SendChatMessage("Ranged has been disabled.");
                                return true;
                            }
                        }
                    }

                    character.UnregisterEventHandler<AttackAllowEvent>(happ!);
                    return false;
                }));
            }

            if (this[Rule.NoMagic])
            {
                EventHappened? happ = null;
                happ = character.RegisterEventHandler(new EventHappened<AttackAllowEvent>(e =>
                {
                    if (character.GetScript<DuelArenaCombatScript>() == script)
                    {
                        if (e.Style == AttackStyle.MagicDefensive || e.Style == AttackStyle.MagicNormal)
                        {
                            {
                                character.SendChatMessage("Magic has been disabled.");
                                return true;
                            }
                        }
                    }

                    character.UnregisterEventHandler<AttackAllowEvent>(happ!);
                    return false;
                }));
            }

            for (var ruleId = 10; ruleId < 24; ruleId++)
            {
                if (this[(Rule)ruleId])
                {
                    var slot = ruleId - 10;
                    EventHappened? happ = null;
                    happ = character.RegisterEventHandler(new EventHappened<EquipAllowEvent>(e =>
                    {
                        if (character.GetScript<DuelArenaCombatScript>() == script)
                        {
                            if (e.Item.EquipmentDefinition.Slot == (EquipmentSlot)slot)
                            {
                                character.SendChatMessage("Equiping this item has been disabled.");
                                return true;
                            }
                        }

                        character.UnregisterEventHandler<EquipAllowEvent>(happ!);
                        return false;
                    }));
                }
            }
        }

        /// <summary>
        ///     Encodes this instance.
        /// </summary>
        /// <returns></returns>
        public DuelRulesDto Dehydrate() => new() { Rules = _rules };

        /// <summary>
        ///     Decodes the specified rules.
        /// </summary>
        /// <param name="rules">The rules.</param>
        public void Hydrate(DuelRulesDto rules) => _rules = rules.Rules;
    }
}