using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for a script that controls the behavior of a Summoning familiar.
    /// </summary>
    public interface IFamiliarScript : INpcScript
    {
        /// <summary>
        /// Gets the character who summoned the familiar.
        /// </summary>
        ICharacter Summoner { get; }
        /// <summary>
        /// Gets the NPC instance that represents the familiar.
        /// </summary>
        INpc Familiar { get; }
        /// <summary>
        /// Gets the target type for the familiar's special move.
        /// </summary>
        /// <returns>The <see cref="FamiliarSpecialType"/> required by the special move.</returns>
        FamiliarSpecialType GetSpecialType();
        /// <summary>
        /// Sets the target for the familiar's special move.
        /// </summary>
        /// <param name="target">The target entity for the special move.</param>
        void SetSpecialMoveTarget(IRuneObject? target);
        /// <summary>
        /// Refreshes the familiar's special move points on the client.
        /// </summary>
        void RefreshSpecialMovePoints();
        /// <summary>
        /// Refreshes the familiar's remaining time on the client.
        /// </summary>
        void RefreshTimer();
        /// <summary>
        /// Teleports the familiar to the summoner's location.
        /// </summary>
        void CallFamiliar();
        /// <summary>
        /// Extends the duration of the familiar's summoning time.
        /// </summary>
        void RenewFamiliar();
        /// <summary>
        /// Initializes the script with the summoner and the familiar's definition data.
        /// </summary>
        /// <param name="summoner">The character who summoned the familiar.</param>
        /// <param name="definition">The data definition for the familiar.</param>
        void InitializeSummoner(ICharacter summoner, SummoningDto definition);
    }
}
