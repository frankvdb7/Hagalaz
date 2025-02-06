using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Services.Model;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface IFamiliarScript : INpcScript
    {
        /// <summary>
        /// Contains the summoner.
        /// </summary>
        ICharacter Summoner { get; }
        /// <summary>
        /// Contains the owner of this script.
        /// </summary>
        INpc Familiar { get; }
        /// <summary>
        /// Gets the type of the special.
        /// </summary>
        /// <returns>SpecialType</returns>
        FamiliarSpecialType GetSpecialType();
        /// <summary>
        /// Sets the special move target.
        /// </summary>
        /// <param name="target">The target.</param>
        void SetSpecialMoveTarget(IRuneObject? target);
        /// <summary>
        /// Refreshe's familiar special move points.
        /// </summary>
        void RefreshSpecialMovePoints();
        /// <summary>
        /// Refreshes the timer.
        /// Every value of 75 is 30 seconds.
        /// </summary>
        void RefreshTimer();
        /// <summary>
        /// Calls the familiar.
        /// </summary>
        void CallFamiliar();
        /// <summary>
        /// Renews the familiar.
        /// </summary>
        void RenewFamiliar();
        /// <summary>
        /// Initializes the specified owner.
        /// </summary>
        /// <param name="summoner">The summoner.</param>
        /// <param name="definition">The definition.</param>
        void InitializeSummoner(ICharacter summoner, SummoningDto definition);
    }
}
