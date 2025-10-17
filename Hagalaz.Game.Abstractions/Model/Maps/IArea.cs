using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Game.Abstractions.Model.Maps
{
    /// <summary>
    /// Defines the contract for a map area, which can have special rules and behaviors.
    /// </summary>
    public interface IArea
    {
        /// <summary>
        /// Gets the unique identifier for the area.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets a value indicating whether this area is a multi-combat zone.
        /// </summary>
        bool MultiCombat { get; }

        /// <summary>
        /// Gets the name of the area.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether PvP (Player vs. Player) combat is enabled in this area.
        /// </summary>
        bool IsPvP { get; }

        /// <summary>
        /// Gets a value indicating whether Summoning familiars are allowed in this area.
        /// </summary>
        bool FamiliarAllowed { get; }

        /// <summary>
        /// Gets the script that controls the area's behavior and logic.
        /// </summary>
        IAreaScript Script { get; }

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
    }
}