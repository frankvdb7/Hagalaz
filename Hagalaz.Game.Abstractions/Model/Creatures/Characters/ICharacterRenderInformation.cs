using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for an object that holds the client-side rendering information for a player character.
    /// </summary>
    public interface ICharacterRenderInformation : ICreatureRenderInformation
    {
        /// <summary>
        /// Gets the combined bitmask of update flags that are currently scheduled for the character.
        /// </summary>
        UpdateFlags UpdateFlag { get; }
        /// <summary>
        /// Gets a value indicating whether the character's item-based appearance (e.g., equipment) needs to be updated on the client.
        /// </summary>
        bool ItemAppearanceUpdateRequired { get; }
        /// <summary>
        /// Gets a value indicating whether the character is in a large scene view, which affects how many entities are rendered.
        /// </summary>
        bool LargeSceneView { get; }
        /// <summary>
        /// Gets or sets the list of NPCs that are currently local to (and potentially visible to) the character.
        /// </summary>
        LinkedList<INpc> LocalNpcs { get; set; }
        /// <summary>
        /// Gets or sets the list of other player characters that are currently local to this character.
        /// </summary>
        LinkedList<ICharacter> LocalCharacters { get; set; }
        /// <summary>
        /// Schedules an update for the character's item-based appearance.
        /// </summary>
        void ScheduleItemAppearanceUpdate();
        /// <summary>
        /// Checks if another character is currently within this character's viewport.
        /// </summary>
        /// <param name="index">The server index of the other character.</param>
        /// <returns><c>true</c> if the other character is in the viewport; otherwise, <c>false</c>.</returns>
        bool IsInViewport(int index);
        /// <summary>
        /// Checks if another character has just entered this character's viewport in the current tick.
        /// </summary>
        /// <param name="index">The server index of the other character.</param>
        /// <returns><c>true</c> if the other character just entered the viewport; otherwise, <c>false</c>.</returns>
        bool HasJustCrossedViewport(int index);
        /// <summary>
        /// Sets the viewport status for another character.
        /// </summary>
        /// <param name="index">The server index of the other character.</param>
        /// <param name="value">The new viewport status.</param>
        void SetInViewport(int index, bool value);
        /// <summary>
        /// Sets the flag indicating that another character has just entered this character's viewport.
        /// </summary>
        /// <param name="index">The server index of the other character.</param>
        /// <param name="value">The new "just crossed" status.</param>
        void SetJustCrossedViewport(int index, bool value);
        /// <summary>
        /// Sets the idle status for another character from the previous update cycle.
        /// </summary>
        /// <param name="index">The server index of the other character.</param>
        /// <param name="value">The idle status to set.</param>
        void SetIdle(int index, bool value);
        /// <summary>
        /// Sets the idle status for another character for the current update cycle.
        /// </summary>
        /// <param name="index">The server index of the other character.</param>
        /// <param name="value">The idle status to set.</param>
        void SetIdleOnThisLoop(int index, bool value);
        /// <summary>
        /// Gets the idle status of another character from the previous update cycle.
        /// </summary>
        /// <param name="index">The server index of the other character.</param>
        /// <returns><c>true</c> if the other character was idle; otherwise, <c>false</c>.</returns>
        bool IsIdle(int index);
        /// <summary>
        /// Gets the idle status of another character for the current update cycle.
        /// </summary>
        /// <param name="index">The server index of the other character.</param>
        /// <returns><c>true</c> if the other character is idle in the current loop; otherwise, <c>false</c>.</returns>
        bool IsIdleOnThisLoop(int index);
        /// <summary>
        /// Asynchronously performs the main update logic for the character's rendering information.
        /// </summary>
        Task Update();
        /// <summary>
        /// Schedules a specific type of appearance update for the character using a bitmask flag.
        /// </summary>
        /// <param name="flag">The update flag to schedule.</param>
        void ScheduleFlagUpdate(UpdateFlags flag);
        /// <summary>
        /// Cancels a previously scheduled appearance update.
        /// </summary>
        /// <param name="flag">The update flag to cancel.</param>
        void CancelScheduledUpdate(UpdateFlags flag);
    }
}
