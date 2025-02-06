using System;
using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.Clans;

namespace Hagalaz.Game.Features.Clans
{
    /// <summary>
    /// 
    /// </summary>
    public class ClanChatChannel
    {
        /// <summary>
        /// A list of members currently active in this clan chat channel.
        /// </summary>
        /// <value>The members.</value>
        private Dictionary<string, IClanMember> _members;

        /// <summary>
        /// Gets the clan to which this channel belongs to.
        /// </summary>
        /// <value>
        /// The clan.
        /// </value>
        public Clan Clan { get; }

        /// <summary>
        /// Gets the members.
        /// </summary>
        /// <value>
        /// The members.
        /// </value>
        public IEnumerable<IClanMember> Members => _members.Values;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClanChatChannel" /> class.
        /// </summary>
        /// <param name="clan">The clan.</param>
        public ClanChatChannel(Clan clan)
        {
            Clan = clan;
            _members = new Dictionary<string, IClanMember>(StringComparer.InvariantCultureIgnoreCase);
        }

        // /// <summary>
        // /// Adds a member to the chat channel.
        // /// </summary>
        // /// <param name="member">The member to add.</param>
        // /// <returns>JoinResponse.</returns>
        // public JoinResponse AddMember(IClanMember member)
        // {
        //     if (Clan.IsBannedMember(member.MasterId))
        //         return JoinResponse.Banned;
        //
        //     // a chat channel is only allow 100 users.
        //     if (_members.Count >= 100)
        //         return JoinResponse.Full;
        //
        //     if (_members.ContainsKey(member.DisplayName))
        //     {
        //         return JoinResponse.Error;
        //     }
        //
        //     if (member.Rank < Clan.Settings.RankToEnterCc)
        //     {
        //         return member.Rank == ClanRank.Guest ? JoinResponse.NotAllowed : JoinResponse.LowRank;
        //     }
        //
        //     _members.Add(member.DisplayName, member);
        //     return JoinResponse.Success;
        //
        // }

        /// <summary>
        /// Removes a member from the chat channel.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool RemoveMember(string name) => _members.Remove(name);
    }
}
