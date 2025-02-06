namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICreatureScript
    {
        /// <summary>
        /// Determines whether this instance [can be looted by] the specified killer.
        /// </summary>
        /// <param name="killer">The killer.</param>
        /// <returns></returns>
        bool CanBeLootedBy(ICreature killer);
        /// <summary>
        /// Get's if this creature can attack the specified creature ('target').
        /// By default , this method returns true.
        /// </summary>
        /// <param name="target">Creature which is being attacked by this npc.</param>
        /// <returns>If attack can be performed.</returns>
        bool CanAttack(ICreature target);
        /// <summary>
        /// Get's if this creature can be attacked by the specified creature ('attacker').
        /// By default , this method does check if this creature is attackable.
        /// This method also checks if the attacker is a character, wether or not it
        /// has the required slayer level.
        /// </summary>
        /// <param name="attacker">Creature which is attacking this creature.</param>
        /// <returns>If attack can be performed.</returns>
        bool CanBeAttackedBy(ICreature attacker);
        /// <summary>
        /// Get's called when creature is dead.
        /// By default, this method does nothing.
        /// </summary>
        void OnDeath();
        /// <summary>
        /// Get's called when creature is killed.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="killer">The killer.</param>
        void OnKilledBy(ICreature killer);
        /// <summary>
        /// Get's called when creature killed its target.
        /// By default, this method does nothing.
        /// </summary>
        /// <param name="target">The target.</param>
        void OnTargetKilled(ICreature target);
        /// <summary>
        /// Get's called when creature is spawned.
        /// By default, this method does nothing.
        /// </summary>
        void OnSpawn();
        /// <summary>
        /// Performs every single tick.
        /// </summary>
        void Tick();
    }
}
