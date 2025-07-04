using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Elementals
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([14303])]
    public class SappingGlacyte : NpcScriptBase
    {
        /// <summary>
        ///     The glacor.
        /// </summary>
        private readonly INpc _glacor;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SappingGlacyte" /> class.
        /// </summary>
        /// <param name="glacor">The glacor.</param>
        public SappingGlacyte(INpc glacor) => _glacor = glacor;

        /// <summary>
        ///     Called when [set target].
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnSetTarget(ICreature target)
        {
            if (_glacor.Combat.Target == null)
            {
                _glacor.QueueTask(new RsTask(() => _glacor.Combat.SetTarget(target), 1));
            }
        }

        /// <summary>
        ///     Get's if this npc can be attacked by the specified character ('attacker').
        ///     By default , this method does check if this npc is attackable.
        ///     This method also checks if the attacker is a character, wether or not it
        ///     has the required slayer level.
        /// </summary>
        /// <param name="attacker">Creature which is attacking this npc.</param>
        /// <returns>
        ///     If attack can be performed.
        /// </returns>
        public override bool CanBeAttackedBy(ICreature attacker)
        {
            if (Owner.Combat.Target == null || Owner.Combat.Target == attacker)
            {
                return base.CanBeAttackedBy(attacker);
            }

            (attacker as ICharacter)?.SendChatMessage("Someone else is already fighting that glacyte.");

            return false;

        }

        /// <summary>
        ///     Get's if this npc can retaliate to specific character attack.
        ///     By default, this method returns true.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///     <c>true</c> if this instance [can retaliate to] the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanRetaliateTo(ICreature creature) => Owner.Combat.Target == null;

        /// <summary>
        ///     Perform's attack on specific target.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void PerformAttack(ICreature target)
        {
            base.PerformAttack(target);
            if (target is ICharacter character)
            {
                character.Statistics.DrainPrayerPoints(20);
            }
        }
    }
}