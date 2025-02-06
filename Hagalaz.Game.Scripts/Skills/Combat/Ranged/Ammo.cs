using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Skills.Combat.Ranged.Bows;
using Hagalaz.Game.Scripts.Skills.Combat.Ranged.Crossbows;

namespace Hagalaz.Game.Scripts.Skills.Combat.Ranged
{
    /// <summary>
    ///     Static class for methods such as ammo dropping.
    /// </summary>
    public static class Ammo
    {
        /// <summary>
        ///     Remove's ammo from specific character where ammoItem is item
        ///     in character's equipment any slot.
        ///     And drops ammo on the ground if required.
        ///     This method can do nothing on certain conditions.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="ammoItem">The ammo item.</param>
        /// <param name="location">The location.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="amount">The amount.</param>
        /// <returns></returns>
        public static bool RemoveAmmo(ICharacter character, IItem ammoItem, ILocation location, int delay, int amount = 1)
        {
            var slot = character.Equipment.GetInstanceSlot(ammoItem);
            if (slot == EquipmentSlot.NoSlot)
            {
                return false;
            }

            if (ammoItem.Count < amount)
            {
                return false;
            }

            var removal = ammoItem.Clone();
            removal.Count = amount;

            if (character.Equipment.Remove(removal, slot) < amount)
            {
                return false;
            }

            var dropChance = 0.75 + character.Statistics.GetSkillLevel(StatisticsConstants.Ranged) * 0.001; // max ~85%
            if (!(RandomStatic.Generator.NextDouble() <= dropChance))
            {
                return true;
            }

            var pickupChance = 0.0;
            if (character.HasState(StateType.AvasAttractorEquiped))
            {
                pickupChance = 0.85;
            }
            else if (character.HasState(StateType.AvasAccumulatorEquiped))
            {
                pickupChance = 0.95;
            }
            else if (character.HasState(StateType.AvasAlerterEquiped))
            {
                pickupChance = 0.98;
            }

            var groundItemBuilder = character.ServiceProvider.GetRequiredService<IGroundItemBuilder>();
            character.QueueTask(new RsTask(() =>
                {
                    if (pickupChance > 0.0 && RandomStatic.Generator.NextDouble() <= pickupChance)
                    {
                        character.Equipment.Add(slot, removal);
                    }
                    else
                    {
                        groundItemBuilder.Create()
                            .WithItem(removal)
                            .WithLocation(location)
                            .WithOwner(character)
                            .Spawn();
                    }
                }, delay));

            return true;
        }

        /// <summary>
        ///     Check's if character has suitable crossbow ammo.
        ///     In some cases of false it sends messages such as "There's no ammo left in your quiver."
        /// </summary>
        /// <returns></returns>
        public static bool HasSuitableCrossbowAmmo(ICharacter character, int amount = 1)
        {
            var ammo = character.Equipment[EquipmentSlot.Arrow];
            if (ammo == null || ammo.Count <= 0)
            {
                character.SendChatMessage("There's no ammo left in your quiver.");
                return false;
            }

            if (ammo.Count < amount)
            {
                character.SendChatMessage("There's not enough ammo left in your quiver.");
                return false;
            }

            var type = Crossbows.Crossbows.GetCrossbowType(character);
            if (type == CrossbowType.None)
            {
                return false;
            }

            var bolt = Bolts.GetBoltType(character);
            if (bolt == BoltType.None || !Lookup(bolt, Crossbows.Crossbows.GetSuitableBoltTypes(type)))
            {
                character.SendChatMessage("This ammo is not suitable with your weapon.");
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Determines whether [has suitable bow ammo] [the specified character].
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="amount">The amount.</param>
        /// <returns>
        ///     <c>true</c> if [has suitable bow ammo] [the specified character]; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasSuitableBowAmmo(ICharacter character, int amount = 1)
        {
            var ammo = character.Equipment[EquipmentSlot.Arrow];
            if (ammo == null || ammo.Count <= 0)
            {
                character.SendChatMessage("There's no ammo left in your quiver.");
                return false;
            }

            if (ammo.Count < amount)
            {
                character.SendChatMessage("There's not enough ammo left in your quiver.");
                return false;
            }

            var type = Bows.Bows.GetBowType(character);
            if (type == BowType.None)
            {
                return false;
            }

            var arrow = Arrows.GetArrowType(character);
            if (arrow == ArrowType.None || !Lookup(arrow, Bows.Bows.GetSuitableArrowTypes(type)))
            {
                character.SendChatMessage("This ammo is not suitable with your weapon.");
                return false;
            }

            return true;
        }


        /// <summary>
        ///     Checks if array contains given value.
        /// </summary>
        private static bool Lookup(BoltType v, BoltType[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] == v)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Checks if array contains given value.
        /// </summary>
        private static bool Lookup(ArrowType v, ArrowType[] array)
        {
            for (var i = 0; i < array.Length; i++)
            {
                if (array[i] == v)
                {
                    return true;
                }
            }

            return false;
        }
    }
}