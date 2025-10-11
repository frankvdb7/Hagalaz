namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the contract for a script that contains the logic and behavior for a creature.
    /// </summary>
    public interface ICreatureScript
    {
        /// <summary>
        /// Determines if this creature's loot can be received by a specific killer.
        /// </summary>
        /// <param name="killer">The creature that killed this creature.</param>
        /// <returns><c>true</c> if the killer is eligible for loot; otherwise, <c>false</c>.</returns>
        bool CanBeLootedBy(ICreature killer);

        /// <summary>
        /// Determines if this creature can attack a specific target.
        /// </summary>
        /// <param name="target">The potential target creature.</param>
        /// <returns><c>true</c> if this creature can attack the target; otherwise, <c>false</c>.</returns>
        bool CanAttack(ICreature target);

        /// <summary>
        /// Determines if this creature can be attacked by a specific attacker.
        /// </summary>
        /// <param name="attacker">The potential attacker.</param>
        /// <returns><c>true</c> if the attack is allowed; otherwise, <c>false</c>.</returns>
        bool CanBeAttackedBy(ICreature attacker);

        /// <summary>
        /// A callback executed when the creature dies.
        /// </summary>
        void OnDeath();

        /// <summary>
        /// A callback executed when this creature is killed by another creature.
        /// </summary>
        /// <param name="killer">The creature that dealt the killing blow.</param>
        void OnKilledBy(ICreature killer);

        /// <summary>
        /// A callback executed when this creature kills another creature.
        /// </summary>
        /// <param name="target">The creature that was killed.</param>
        void OnTargetKilled(ICreature target);

        /// <summary>
        /// A callback executed when the creature is first spawned into the game world.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// Processes a single game tick for the creature's script logic.
        /// </summary>
        void Tick();
    }
}
