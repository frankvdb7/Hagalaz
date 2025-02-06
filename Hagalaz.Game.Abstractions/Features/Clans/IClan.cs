using System.Collections.Generic;

namespace Hagalaz.Game.Abstractions.Features.Clans
{
    /// <summary>
    /// 
    /// </summary>
    public interface IClan
    {
        /// <summary>
        /// Contains the clan name.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Gets the members.
        /// </summary>
        /// <value>
        /// The members.
        /// </value>
        IEnumerable<IClanMember> Members { get; }
        /// <summary>
        /// Contains the clan settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        IClanSettings Settings { get; set; }
        /// <summary>
        /// Gets the banned members.
        /// </summary>
        /// <value>
        /// The banned members.
        /// </value>
        IReadOnlyDictionary<uint, string> BannedMembers { get; }
        /// <summary>
        /// Updates the members.
        /// </summary>
        /// <param name="members">The members.</param>
        void SetMembers(Dictionary<uint, IClanMember> members);
        /// <summary>
        /// Sets the banned members.
        /// </summary>
        /// <param name="bannedMembers">The banned members.</param>
        void SetBannedMembers(Dictionary<uint, string> bannedMembers);
        /// <summary>
        /// Gets the member.
        /// </summary>
        /// <param name="masterID">The master identifier.</param>
        /// <returns></returns>
        IClanMember? GetMember(uint masterID);
        /// <summary>
        /// Gets the index of the member by.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        IClanMember? GetMemberByIndex(int index);
    }
}
