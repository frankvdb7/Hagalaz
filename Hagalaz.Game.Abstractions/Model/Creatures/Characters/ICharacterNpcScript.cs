using Hagalaz.Game.Abstractions.Model.Combat;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for a script that controls a player character's behavior while they are transformed into an NPC (Player-NPC).
    /// </summary>
    public interface ICharacterNpcScript
    {
        /// <summary>
        /// Initializes the script with the character that it is attached to.
        /// </summary>
        /// <param name="owner">The character that owns this script.</param>
        void Initialize(ICharacter owner);
        /// <summary>
        /// A callback executed when the character transforms into the NPC associated with this script.
        /// </summary>
        void OnTurnToNpc();
        /// <summary>
        /// A callback executed when the character reverts from the NPC transformation back to their player appearance.
        /// </summary>
        void OnTurnToPlayer();
        /// <summary>
        /// A callback executed when the transformed character dies.
        /// </summary>
        void OnDeath();
        /// <summary>
        /// A callback executed when the transformed character spawns.
        /// </summary>
        void OnSpawn();
        /// <summary>
        /// Gets the attack distance for the transformed character.
        /// </summary>
        /// <returns>The attack distance in tiles.</returns>
        int GetAttackDistance();
        /// <summary>
        /// Gets the attack speed for the transformed character.
        /// </summary>
        /// <returns>The attack speed in game ticks.</returns>
        int GetAttackSpeed();
        /// <summary>
        /// Gets the maximum life points for the transformed character.
        /// </summary>
        /// <returns>The maximum life points.</returns>
        int GetMaximumHitpoints();
        /// <summary>
        /// Gets the attack bonus type for the transformed character.
        /// </summary>
        /// <returns>The <see cref="AttackBonus"/> type.</returns>
        AttackBonus GetAttackBonusType();
        /// <summary>
        /// Gets the attack style for the transformed character.
        /// </summary>
        /// <returns>The <see cref="AttackStyle"/>.</returns>
        AttackStyle GetAttackStyle();
        /// <summary>
        /// Gets the display name for the transformed character.
        /// </summary>
        /// <returns>The display name.</returns>
        string GetDisplayName();
        /// <summary>
        /// Gets the combat level for the transformed character.
        /// </summary>
        /// <returns>The combat level.</returns>
        int GetCombatLevel();
        /// <summary>
        /// Defines the logic for the transformed character's attack on a target.
        /// </summary>
        /// <param name="target">The creature to be attacked.</param>
        void PerformAttack(ICreature target);
        /// <summary>
        /// A callback executed when the transformed character kills a target.
        /// </summary>
        /// <param name="target">The creature that was killed.</param>
        void OnTargetKilled(ICreature target);
        /// <summary>
        /// Determines if the transformed character's loot can be claimed by a specific killer.
        /// </summary>
        /// <param name="killer">The creature that killed the transformed character.</param>
        /// <returns><c>true</c> if the killer can receive loot; otherwise, <c>false</c>.</returns>
        bool CanBeLootedBy(ICreature killer);
        /// <summary>
        /// A callback executed when the transformed character is killed by another creature.
        /// </summary>
        /// <param name="killer">The creature that dealt the killing blow.</param>
        void OnDeathBy(ICreature killer);
        /// <summary>
        /// Defines the death sequence for the transformed character.
        /// </summary>
        /// <returns>The number of game ticks to wait before the character respawns.</returns>
        int RenderDeath();
        /// <summary>
        /// Defines the defensive animation or effect for the transformed character when attacked.
        /// </summary>
        /// <param name="delay">The delay in client ticks until the incoming attack hits.</param>
        void RenderDefence(int delay);
        /// <summary>
        /// Processes a single game tick of logic for the script.
        /// </summary>
        void Tick();
    }
}
