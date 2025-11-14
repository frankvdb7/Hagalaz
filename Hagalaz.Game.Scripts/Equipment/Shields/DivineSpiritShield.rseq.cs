using System;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Equipment.Shields
{
    /// <summary>
    /// </summary>
    [EquipmentScriptMetaData([13740])]
    public class DivineSpiritShield : EquipmentScript
    {
        /// <summary>
        ///     Happens when incomming attack is performed to victim from attacker.
        ///     By default this method does nothing and return's damage in parameters.
        /// </summary>
        /// <param name="item">Item in equipment instance.</param>
        /// <param name="victim">Character which is being attacked.</param>
        /// <param name="attacker">Creture which is attacking victim.</param>
        /// <param name="damageType">Type of the damage that is being received.</param>
        /// <param name="damage">Amount of damage, can be -1 incase of miss.</param>
        /// <param name="delay">Delay in client ticks until the attack will reach the target.</param>
        /// <returns>
        ///     System.Int32.
        /// </returns>
        public override int OnIncomingAttack(IItem item, ICharacter victim, ICreature attacker, DamageType damageType, int damage, int delay)
        {
            var drain = (int)(Math.Ceiling(damage * 0.30) * 0.50);
            if (victim.Statistics.PrayerPoints >= drain)
            {
                victim.Statistics.DrainPrayerPoints(drain);
                return (int)(damage * 0.70); // 30% damage reduction
            }

            return damage;
        }
    }
}