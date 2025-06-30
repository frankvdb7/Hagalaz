using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Npcs.Elementals
{
    /// <summary>
    /// </summary>
    [NpcScriptMetaData([14304])]
    public class EnduringGlacyte : NpcScriptBase
    {
        /// <summary>
        ///     The glacor
        /// </summary>
        private readonly INpc _glacor;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EnduringGlacyte" /> class.
        /// </summary>
        /// <param name="glacor">The glacor.</param>
        public EnduringGlacyte(INpc glacor) => _glacor = glacor;

        /// <summary>
        ///     Called when [set target].
        ///     By default, this method does nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnSetTarget(ICreature target)
        {
            if (_glacor.Combat.Target == null)
            {
                _glacor.QueueTask(() => _glacor.Combat.SetTarget(target), 1);
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
        ///     Happens when attacker starts attack to this npc.
        ///     By default this method does nothing and returns the damage provided in parameters.
        /// </summary>
        /// <param name="attacker">Creature which started the attack.</param>
        /// <param name="damageType">Type of the attack.</param>
        /// <param name="damage">Amount of damage that is predicted to be inflicted or -1 if it's a miss.</param>
        /// <param name="delay">Delay in client ticks until the attack will reach this npc and OnAttack will be called.</param>
        /// <returns>
        ///     Return's amount of damage that remains after defence.
        /// </returns>
        public override int OnIncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay)
        {
            var distance = Owner.Location.GetDistance(_glacor.Location);
            const double maxDistance = 14.0;
            return (int)(distance / maxDistance * damage);

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
    }
}