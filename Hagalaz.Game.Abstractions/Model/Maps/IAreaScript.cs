using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Defines the contract for a script that controls the behavior and rules of a map area.
    /// </summary>
    public interface IAreaScript
    {
        /// <summary>
        /// Initializes the script with its owning area.
        /// </summary>
        /// <param name="area">The area that this script is attached to.</param>
        void Initialize(IArea area);

        /// <summary>
        /// A callback executed when a creature enters this area.
        /// </summary>
        /// <param name="creature">The creature that entered the area.</param>
        void OnCreatureEnterArea(ICreature creature);

        /// <summary>
        /// A callback executed when a creature exits this area.
        /// </summary>
        /// <param name="creature">The creature that exited the area.</param>
        void OnCreatureExitArea(ICreature creature);

        /// <summary>
        /// A callback executed when a creature's client needs to render the effects of entering this area.
        /// </summary>
        /// <param name="creature">The creature entering the area.</param>
        void RenderEnterArea(ICreature creature);

        /// <summary>
        /// A callback executed when a creature's client needs to render the effects of exiting this area.
        /// </summary>
        /// <param name="creature">The creature exiting the area.</param>
        void RenderExitArea(ICreature creature);

        /// <summary>
        /// Checks if a creature within this area can be attacked by another creature.
        /// </summary>
        /// <param name="victim">The creature being attacked (in this area).</param>
        /// <param name="attacker">The creature performing the attack.</param>
        /// <returns><c>true</c> if the attack is allowed; otherwise, <c>false</c>.</returns>
        bool CanBeAttacked(ICreature victim, ICreature attacker);

        /// <summary>
        /// Checks if a creature within this area can attack another creature.
        /// </summary>
        /// <param name="attacker">The creature performing the attack (in this area).</param>
        /// <param name="target">The creature being targeted.</param>
        /// <returns><c>true</c> if the attack is allowed; otherwise, <c>false</c>.</returns>
        bool CanAttack(ICreature attacker, ICreature target);

        /// <summary>
        /// Checks if a character can perform a standard spellbook teleport while in this area.
        /// </summary>
        /// <param name="character">The character attempting to teleport.</param>
        /// <returns><c>true</c> if teleportation is allowed; otherwise, <c>false</c>.</returns>
        bool CanDoStandardTeleport(ICharacter character);

        /// <summary>
        /// Checks if a character can use a game object to teleport while in this area.
        /// </summary>
        /// <param name="character">The character attempting to teleport.</param>
        /// <param name="obj">The game object being used for teleportation.</param>
        /// <returns><c>true</c> if teleportation is allowed; otherwise, <c>false</c>.</returns>
        bool CanDoGameObjectTeleport(ICharacter character, IGameObject obj);

        /// <summary>
        /// Gets the designated respawn location for a character who dies in this area.
        /// </summary>
        /// <param name="character">The character who died.</param>
        /// <returns>The respawn <see cref="ILocation"/>.</returns>
        ILocation GetRespawnLocation(ICharacter character);

        /// <summary>
        /// Gets the combat level range for PvP encounters in this area.
        /// </summary>
        /// <param name="character">The character in the area.</param>
        /// <returns>The combat level range.</returns>
        int GetPvPCombatLevelRange(ICharacter character);

        /// <summary>
        /// Determines if the client should render a character's base combat level separately from their Summoning-boosted level.
        /// </summary>
        /// <param name="character">The character to check.</param>
        /// <returns><c>true</c> if the base combat level should be rendered separately; otherwise, <c>false</c>.</returns>
        bool ShouldRenderBaseCombatLevel(ICharacter character);
    }
}