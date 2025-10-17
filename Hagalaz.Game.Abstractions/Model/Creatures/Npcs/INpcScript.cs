using System;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Npcs
{
    /// <summary>
    /// Defines the contract for a script that controls an NPC's behavior and logic.
    /// </summary>
    public interface INpcScript : ICreatureScript
    {
        /// <summary>
        /// Gets the NPC IDs that this script is suitable for.
        /// </summary>
        /// <returns>An array of NPC IDs.</returns>
        [Obsolete("Please use NpcScriptMetaData attribute instead")]
        int[] GetSuitableNpcs();

        /// <summary>
        /// Gets the pathfinder the NPC will use.
        /// </summary>
        /// <returns>The <see cref="IPathFinder"/> for the NPC.</returns>
        IPathFinder GetPathfinder();

        /// <summary>
        /// Respawns the NPC.
        /// </summary>
        void Respawn();

        /// <summary>
        /// Determines whether this NPC can respawn.
        /// </summary>
        /// <returns><c>true</c> if the NPC can respawn; otherwise, <c>false</c>.</returns>
        bool CanRespawn();

        /// <summary>
        /// Renders the NPC's death animation and effects.
        /// </summary>
        /// <returns>The number of game ticks the death sequence will last.</returns>
        int RenderDeath();

        /// <summary>
        /// Renders the NPC's defence animation and effects.
        /// </summary>
        /// <param name="delay">The delay in client ticks until the attack hits.</param>
        void RenderDefence(int delay);

        /// <summary>
        /// Initializes the script with its owning NPC.
        /// </summary>
        /// <param name="owner">The NPC that this script is attached to.</param>
        void Initialize(INpc owner);

        /// <summary>
        /// A callback executed when an attacker's hit connects with the NPC.
        /// </summary>
        /// <param name="attacker">The creature that performed the attack.</param>
        /// <param name="damageType">The type of damage dealt.</param>
        /// <param name="damage">The amount of damage inflicted, or -1 for a miss.</param>
        /// <returns>The final damage amount after any script-specific modifications.</returns>
        int OnAttack(ICreature attacker, DamageType damageType, int damage);

        /// <summary>
        /// A callback executed when an attacker initiates an attack on this NPC.
        /// </summary>
        /// <param name="attacker">The creature that initiated the attack.</param>
        /// <param name="damageType">The type of damage to be dealt.</param>
        /// <param name="damage">The predicted amount of damage, or -1 for a miss.</param>
        /// <param name="delay">The delay in client ticks until the attack connects.</param>
        /// <returns>The predicted damage amount after any script-specific modifications.</returns>
        int OnIncomingAttack(ICreature attacker, DamageType damageType, int damage, int delay);

        /// <summary>
        /// Determines if this NPC should retaliate against a specific attacker.
        /// </summary>
        /// <param name="creature">The creature that attacked the NPC.</param>
        /// <returns><c>true</c> if the NPC should retaliate; otherwise, <c>false</c>.</returns>
        bool CanRetaliateTo(ICreature creature);

        /// <summary>
        /// A callback executed when a character clicks on this NPC.
        /// </summary>
        /// <param name="clicker">The character who clicked the NPC.</param>
        /// <param name="clickType">The type of click option selected.</param>
        /// <param name="forceRun">A value indicating whether the character should force-run to the NPC.</param>
        void OnCharacterClick(ICharacter clicker, NpcClickType clickType, bool forceRun);

        /// <summary>
        /// Performs an attack on a specific target.
        /// </summary>
        /// <param name="target">The creature to attack.</param>
        void PerformAttack(ICreature target);

        /// <summary>
        /// A callback executed after the NPC has performed an attack on a target.
        /// </summary>
        /// <param name="target">The creature that was attacked.</param>
        void OnAttackPerformed(ICreature target);

        /// <summary>
        /// A callback executed when the NPC sets a new combat target.
        /// </summary>
        /// <param name="target">The new target.</param>
        void OnSetTarget(ICreature target);

        /// <summary>
        /// Determines if the NPC can set a specific creature as its combat target.
        /// </summary>
        /// <param name="target">The potential target.</param>
        /// <returns><c>true</c> if the target can be set; otherwise, <c>false</c>.</returns>
        bool CanSetTarget(ICreature target);

        /// <summary>
        /// A callback executed when the NPC's combat target is cleared.
        /// </summary>
        void OnCancelTarget();

        /// <summary>
        /// Gets the attack bonus type for the NPC's current combat style.
        /// </summary>
        /// <returns>The <see cref="AttackBonus"/> type.</returns>
        AttackBonus GetAttackBonusType();

        /// <summary>
        /// Gets the attack style for the NPC.
        /// </summary>
        /// <returns>The <see cref="AttackStyle"/>.</returns>
        AttackStyle GetAttackStyle();

        /// <summary>
        /// Gets the attack distance for the NPC.
        /// </summary>
        /// <returns>The attack distance in tiles.</returns>
        int GetAttackDistance();

        /// <summary>
        /// Gets the attack speed for the NPC.
        /// </summary>
        /// <returns>The attack speed in game ticks.</returns>
        int GetAttackSpeed();

        /// <summary>
        /// Determines if this NPC can be poisoned.
        /// </summary>
        /// <returns><c>true</c> if the NPC is poisonable; otherwise, <c>false</c>.</returns>
        bool CanPoison();

        /// <summary>
        /// Determines if this NPC can spawn into the world.
        /// </summary>
        /// <returns><c>true</c> if the NPC can spawn; otherwise, <c>false</c>.</returns>
        bool CanSpawn();

        /// <summary>
        /// Determines if this NPC can be permanently destroyed.
        /// </summary>
        /// <returns><c>true</c> if the NPC can be destroyed; otherwise, <c>false</c>.</returns>
        bool CanDestroy();

        /// <summary>
        /// Determines if this NPC's processing can be suspended (e.g., when no players are nearby).
        /// </summary>
        /// <returns><c>true</c> if the NPC can be suspended; otherwise, <c>false</c>.</returns>
        bool CanSuspend();

        /// <summary>
        /// Handles the "Use item on NPC" action.
        /// </summary>
        /// <param name="used">The item being used on the NPC.</param>
        /// <param name="character">The character using the item.</param>
        /// <returns><c>true</c> if the action was handled by the script; otherwise, <c>false</c>.</returns>
        bool UseItemOnNpc(IItem used, ICharacter character);

        /// <summary>
        /// A callback executed when the NPC's current action is interrupted.
        /// </summary>
        /// <param name="source">The object or system that caused the interruption.</param>
        void OnInterrupt(object source);

        /// <summary>
        /// A callback executed when the NPC is permanently destroyed.
        /// </summary>
        void OnDestroy();

        /// <summary>
        /// A callback executed when the NPC is first created.
        /// </summary>
        void OnCreate();
    }
}