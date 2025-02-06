using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Class for character magic.
    /// </summary>
    public class Magic : IMagic
    {
        /// <summary>
        /// Contains owner of this class.
        /// </summary>
        private readonly ICharacter _owner;
        /// <summary>
        /// Contains spell that is being autocasted.
        /// </summary>
        public ICombatSpell? AutoCastingSpell { get; private set; }
        /// <summary>
        /// Contains spell that was select by 'use on'.
        /// </summary>
        public ICombatSpell? SelectedSpell { get; set; }

        /// <summary>
        /// Construct's new magic class with given owner.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public Magic(ICharacter owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Set's autocasted spell.
        /// </summary>
        public void SetAutoCastingSpell(ICombatSpell spell, bool inform = true)
        {
            if (AutoCastingSpell == spell)
                return;
            AutoCastingSpell?.OnAutoCastingDeactivation(_owner);
            AutoCastingSpell = spell;
            spell.OnAutoCastingActivation(_owner);
            _owner.Mediator.Publish(new ProfileSetIntAction(ProfileConstants.CombatSettingsAttackStyleOptionId, byte.MaxValue));

            if (inform)
            {
                _owner.SendChatMessage("Autocast spell selected.");
            }
        }

        /// <summary>
        /// Clear's autocasted spell.
        /// </summary>
        public void ClearAutoCastingSpell(bool inform = true)
        {
            if (AutoCastingSpell == null)
            {
                return;
            }

            AutoCastingSpell.OnAutoCastingDeactivation(_owner);
            AutoCastingSpell = null;
            _owner.Mediator.Publish(new ProfileSetIntAction(ProfileConstants.CombatSettingsAttackStyleOptionId, 0));

            if (inform)
                _owner.SendChatMessage("Autocast spell cleared.");
        }

        /// <summary>
        /// Check's rune requirements.
        /// Both arrays lengths must math.
        /// Sends message 'You don't have enough x runes to cast this spell.'
        /// </summary>
        /// <param name="types">Types of the runes.</param>
        /// <param name="runeAmounts">The rune amounts.</param>
        /// <returns></returns>
        public bool CheckRunes(RuneType[] types, int[] runeAmounts)
        {
            // Prevent the rune amounts instance of being edited.
            var amounts = new int[runeAmounts.Length];
            runeAmounts.CopyTo(amounts, 0);

            var weapon = _owner.Equipment[EquipmentSlot.Weapon];

            if (weapon != null)
            {
                for (var i = 0; i < types.Length; i++)
                {
                    if (HasInfiniteRunes(types[i], weapon))
                    {
                        amounts[i] = 0;
                    }
                }
            }

            for (var i = 0; i < _owner.Inventory.Capacity; i++)
            {
                var item = _owner.Inventory[i];
                if (item == null)
                    continue;

                var left = item.Count;

                for (var a = 0; a < types.Length; a++)
                {
                    if (amounts[a] == 0)
                        continue;

                    if (!HasRune(types[a], item))
                    {
                        continue;
                    }

                    if (left > amounts[a])
                    {
                        left -= amounts[a];
                        amounts[a] = 0;
                    }
                    else
                    {
                        amounts[a] -= left;
                        left = 0;
                        break;
                    }
                }
            }

            for (var i = 0; i < amounts.Length; i++)
            {
                if (amounts[i] <= 0)
                {
                    continue;
                }

                _owner.SendChatMessage("You don't have enough " + _owner.ServiceProvider.GetRequiredService<IItemService>().FindItemDefinitionById((int)types[i]).Name.ToLower() + "s" + " to cast this spell.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Determines whether [has infinite runes] [the specified rune].
        /// </summary>
        /// <param name="rune">The rune.</param>
        /// <param name="weapon">The weapon.</param>
        /// <returns>
        ///   <c>true</c> if [has infinite runes] [the specified rune]; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasInfiniteRunes(RuneType rune, IItem weapon)
        {
            switch (rune)
            {
                case RuneType.Air:
                    return weapon.Id == (int)StaffType.AirStaff || weapon.Id == (int)StaffType.AirBattleStaff ||
                           weapon.Id == (int)StaffType.MysticAirStaff || weapon.Id == (int)StaffType.ArmadylBattleStaff ||
                           weapon.Id == (int)StaffType.DominionStaff;
                case RuneType.Water:
                    return weapon.Id == (int)StaffType.WaterStaff || weapon.Id == (int)StaffType.WaterBattleStaff ||
                           weapon.Id == (int)StaffType.MysticWaterStaff || weapon.Id == (int)StaffType.MudBattleStaff ||
                           weapon.Id == (int)StaffType.SteamBattleStaff || weapon.Id == (int)StaffType.MysticMudStaff ||
                           weapon.Id == (int)StaffType.MysticSteamStaff || weapon.Id == (int)StaffType.DominionStaff;
                case RuneType.Earth:
                    return weapon.Id == (int)StaffType.EarthStaff || weapon.Id == (int)StaffType.EarthBattleStaff ||
                           weapon.Id == (int)StaffType.MysticEarthStaff || weapon.Id == (int)StaffType.MudBattleStaff ||
                           weapon.Id == (int)StaffType.LavaBattleStaff || weapon.Id == (int)StaffType.MysticMudStaff ||
                           weapon.Id == (int)StaffType.MysticLavaStaff || weapon.Id == (int)StaffType.DominionStaff;
                case RuneType.Fire:
                    return weapon.Id == (int)StaffType.FireStaff || weapon.Id == (int)StaffType.FireBattleStaff ||
                           weapon.Id == (int)StaffType.MysticFireStaff || weapon.Id == (int)StaffType.SteamBattleStaff ||
                           weapon.Id == (int)StaffType.LavaBattleStaff || weapon.Id == (int)StaffType.MysticSteamStaff ||
                           weapon.Id == (int)StaffType.MysticLavaStaff || weapon.Id == (int)StaffType.DominionStaff;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Checks the advanced rune types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if the specified type has rune; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasRune(RuneType type, IItem item)
        {
            switch (type)
            {
                case RuneType.Air:
                    return item.Id == (int)RuneType.Air || item.Id == (int)AdvancedRuneType.Smoke ||
                           item.Id == (int)AdvancedRuneType.Mist || item.Id == (int)AdvancedRuneType.Dust;
                case RuneType.Water:
                    return item.Id == (int)RuneType.Water || item.Id == (int)AdvancedRuneType.Mist ||
                           item.Id == (int)AdvancedRuneType.Mud || item.Id == (int)AdvancedRuneType.Steam;
                case RuneType.Earth:
                    return item.Id == (int)RuneType.Earth || item.Id == (int)AdvancedRuneType.Dust ||
                           item.Id == (int)AdvancedRuneType.Mud || item.Id == (int)AdvancedRuneType.Lava;
                case RuneType.Fire:
                    return item.Id == (int)RuneType.Fire || item.Id == (int)AdvancedRuneType.Smoke ||
                           item.Id == (int)AdvancedRuneType.Steam || item.Id == (int)AdvancedRuneType.Lava;
                default:
                    return (int)type == item.Id;
            }
        }

        /// <summary>
        /// Remove's runes from character's inventory.
        /// This method doesn't throw exceptions if there's not enough runes
        /// so calling CheckRunes must be called.
        /// </summary>
        /// <param name="types">The types.</param>
        /// <param name="runeAmounts">The rune amounts.</param>
        public void RemoveRunes(RuneType[] types, int[] runeAmounts)
        {
            // Prevent the rune amounts instance of being edited.
            var amounts = new int[runeAmounts.Length];
            runeAmounts.CopyTo(amounts, 0);

            var weapon = _owner.Equipment[EquipmentSlot.Weapon];

            if (weapon != null)
            {
                for (var i = 0; i < types.Length; i++)
                {
                    if (HasInfiniteRunes(types[i], weapon))
                    {
                        amounts[i] = 0;
                    }
                }
            }

            for (var slot = 0; slot < _owner.Inventory.Capacity; slot++)
            {
                var item = _owner.Inventory[slot];
                if (item == null)
                    continue;

                for (var a = 0; a < types.Length; a++)
                {
                    if (amounts[a] <= 0)
                        continue;

                    if (!HasRune(types[a], item))
                    {
                        continue;
                    }

                    if (item.Count > amounts[a])
                    {
                        var newItem = item.Clone();
                        newItem.Count = item.Count - amounts[a];
                        item = newItem;
                        _owner.Inventory.Replace(slot, newItem);
                        amounts[a] = 0;
                    }
                    else
                    {
                        amounts[a] -= item.Count;
                        _owner.Inventory.Remove(item, slot);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Checks the magic level.
        /// </summary>
        /// <param name="required">The required.</param>
        /// <returns></returns>
        public bool CheckMagicLevel(int required)
        {
            if (_owner.Statistics.GetSkillLevel(StatisticsConstants.Magic) >= required || _owner.Statistics.LevelForExperience(StatisticsConstants.Magic) >= required)
            {
                return true;
            }

            _owner.SendChatMessage("Your magic level is not high enough for this spell.");
            return false;
        }
    }
}
