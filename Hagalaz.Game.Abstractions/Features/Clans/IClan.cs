using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Features.Clans
{
    /// <summary>
    /// Defines the contract for a clan, which represents a group of players.
    /// This interface provides access to the clan's name, members, settings, and banned list.
    /// </summary>
    public interface IClan
    {
        /// <summary>
        /// Gets the name of the clan.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets an enumerable collection of all members in the clan.
        /// </summary>
        IEnumerable<IClanMember> Members { get; }

        /// <summary>
        /// Gets or sets the settings for the clan, which control various permissions and behaviors.
        /// </summary>
        IClanSettings Settings { get; set; }

        /// <summary>
        /// Gets a read-only dictionary of all banned members, mapping their unique ID to their display name.
        /// </summary>
        IReadOnlyDictionary<uint, string> BannedMembers { get; }

        /// <summary>
        /// Replaces the current list of clan members with a new one.
        /// </summary>
        /// <param name="members">A dictionary containing the new members, mapping their unique ID to their <see cref="IClanMember"/> object.</param>
        void SetMembers(Dictionary<uint, IClanMember> members);

        /// <summary>
        /// Replaces the current list of banned members with a new one.
        /// </summary>
        /// <param name="bannedMembers">A dictionary containing the new banned members, mapping their unique ID to their display name.</param>
        void SetBannedMembers(Dictionary<uint, string> bannedMembers);

        /// <summary>
        /// Retrieves a specific clan member by their unique identifier.
        /// </summary>
        /// <param name="masterID">The unique identifier of the member to retrieve.</param>
        /// <returns>The <see cref="IClanMember"/> object if found; otherwise, <c>null</c>.</returns>
        IClanMember? GetMember(uint masterID);

        /// <summary>
        /// Retrieves a clan member by their index in the clan's member list.
        /// </summary>
        /// <param name="index">The zero-based index of the member to retrieve.</param>
        /// <returns>The <see cref="IClanMember"/> at the specified index, or <c>null</c> if the index is out of range.</returns>
        IClanMember? GetMemberByIndex(int index);
    }
}
