using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICharacterRenderInformation : ICreatureRenderInformation
    {
        /// <summary>
        /// Contains character update flag.
        /// </summary>
        /// <value>The update flag.</value>
        UpdateFlags UpdateFlag { get; }
        /// <summary>
        /// Gets a value indicating whether [item appearance update required].
        /// </summary>
        /// <value>
        /// <c>true</c> if [item appearance update required]; otherwise, <c>false</c>.
        /// </value>
        bool ItemAppearanceUpdateRequired { get; }
        /// <summary>
        /// Gets a value indicating whether [large scene view].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [large scene view]; otherwise, <c>false</c>.
        /// </value>
        bool LargeSceneView { get; }
        /// <summary>
        /// Contains local NPCS.
        /// </summary>
        /// <value>The local NPCS.</value>
        LinkedList<INpc> LocalNpcs { get; set; }
        /// <summary>
        /// Contains visible local characters.
        /// </summary>
        /// <value>The local characters.</value>
        LinkedList<ICharacter> LocalCharacters { get; set; }
        /// <summary>
        /// Shedules the item appearance update.
        /// </summary>
        void ScheduleItemAppearanceUpdate();
        /// <summary>
        /// Get's if character is in screen.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if [is in viewport] [the specified index]; otherwise, <c>false</c>.</returns>
        bool IsInViewport(int index);
        /// <summary>
        /// Get's if character just showed up on screen.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool HasJustCrossedViewport(int index);
        /// <summary>
        /// Set's if player is in screen.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        void SetInViewport(int index, bool value);
        /// <summary>
        /// Set's if player just showed up on screen.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        void SetJustCrossedViewport(int index, bool value);
        /// <summary>
        /// Set's that character skipped last cycle.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        void SetIdle(int index, bool value);
        /// <summary>
        /// Set's that character this cycle.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="value">if set to <c>true</c> [value].</param>
        void SetIdleOnThisLoop(int index, bool value);
        /// <summary>
        /// Get's if character skipped last cycle.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if the specified index is idle; otherwise, <c>false</c>.</returns>
        bool IsIdle(int index);
        /// <summary>
        /// Get's if character skipped this cycle.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if [is idle on this loop] [the specified index]; otherwise, <c>false</c>.</returns>
        bool IsIdleOnThisLoop(int index);
        /// <summary>
        /// Updates this instance.
        /// </summary>
        Task Update();
        /// <summary>
        /// Shedule's flag based update to character.
        /// </summary>
        /// <param name="flag">The flag.</param>
        void ScheduleFlagUpdate(UpdateFlags flag);
        /// <summary>
        /// Cancel's sheduled flag update.
        /// </summary>
        /// <param name="flag">The flag.</param>
        void CancelScheduledUpdate(UpdateFlags flag);
    }
}
