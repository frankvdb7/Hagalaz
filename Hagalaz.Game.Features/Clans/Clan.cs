using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Features.Clans;

namespace Hagalaz.Game.Features.Clans
{
    /// <summary>
    /// 
    /// </summary>
    public class Clan : IClan
    {
        /// <summary>
        /// The maximum members
        /// </summary>
        public const ushort MaxMembers = 500;
        /// <summary>
        /// The maximum banned members
        /// </summary>
        public const ushort MaxBannedMembers = 100;

        /// <summary>
        /// A dictionary of members currently in this clan.
        /// </summary>
        /// <value>The members.</value>
        private Dictionary<uint, IClanMember> _members;
        /// <summary>
        /// Contains the banned members.
        /// </summary>
        private Dictionary<uint, string> _bannedMembers;
        /// <summary>
        /// Contains the settings.
        /// </summary>
        private IClanSettings _settings;

        /// <summary>
        /// Contains the clan identifier.
        /// </summary>
        public uint Id { get; private set; }

        /// <summary>
        /// Contains the clan name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Contains the clan settings.
        /// </summary>
        /// <value>
        /// The settings.
        /// </value>
        public IClanSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                _settings.ClanId = Id;
            }
        }

        /// <summary>
        /// Contains the chat channel.
        /// </summary>
        public ClanChatChannel ChatChannel { get; }

        /// <summary>
        /// Gets the members.
        /// </summary>
        /// <value>
        /// The members.
        /// </value>
        public IEnumerable<IClanMember> Members => _members.Values;

        /// <summary>
        /// Gets the banned members.
        /// </summary>
        /// <value>
        /// The banned members.
        /// </value>
        public IReadOnlyDictionary<uint, string> BannedMembers => _bannedMembers;

        /// <summary>
        /// Initializes a new instance of the <see cref="Clan" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public Clan(string name)
        {
            Name = name;
            Settings = new ClanSettings();
            ChatChannel = new ClanChatChannel(this);
            _members = new Dictionary<uint, IClanMember>(MaxMembers);
            _bannedMembers = new Dictionary<uint, string>(MaxBannedMembers);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Clan" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="members">The members.</param>
        /// <param name="bannedMembers">The banned members.</param>
        public Clan(string name, Dictionary<uint, IClanMember> members, Dictionary<uint, string> bannedMembers)
        {
            Name = name;
            _members = members;
            _bannedMembers = bannedMembers;
        }

        /// <summary>
        /// Updates the members.
        /// </summary>
        /// <param name="members">The members.</param>
        public void SetMembers(Dictionary<uint, IClanMember> members) => _members = members;

        /// <summary>
        /// Sets the banned members.
        /// </summary>
        /// <param name="bannedMembers">The banned members.</param>
        public void SetBannedMembers(Dictionary<uint, string> bannedMembers) => _bannedMembers = bannedMembers;

        // /// <summary>
        // /// Adds a member to the clan.
        // /// </summary>
        // /// <param name="member">The member to add.</param>
        // /// <returns>JoinResponse.</returns>
        // public JoinResponse AddMember(IClanMember member)
        // {
        //     // clan only allow up to 500 members
        //     if (_members.Count >= 500)
        //         return JoinResponse.Full;
        //     if (_members.ContainsKey(member.MasterId))
        //     {
        //         return JoinResponse.Error;
        //     }
        //
        //     _members.Add(member.MasterId, member);
        //     return JoinResponse.Success;
        // }

        /// <summary>
        /// Gets the member.
        /// </summary>
        /// <param name="masterID">The master identifier.</param>
        /// <returns></returns>
        public IClanMember? GetMember(uint masterID)
        {
            if (_members.ContainsKey(masterID))
                return _members[masterID];
            return null;
        }

        /// <summary>
        /// Gets the index of the member by.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public IClanMember? GetMemberByIndex(int index)
        {
            if (index < 0 || index >= _members.Count)
                return null;
            var members = new List<IClanMember>(Members);
            return members[index];
        }

        /// <summary>
        /// Determines whether the specified master identifier has member.
        /// </summary>
        /// <param name="masterID">The master identifier.</param>
        /// <returns></returns>
        public bool HasMember(uint masterID) => _members.ContainsKey(masterID);

        /// <summary>
        /// Determines whether [is banned member] [the specified master identifier].
        /// </summary>
        /// <param name="masterID">The master identifier.</param>
        /// <returns></returns>
        public bool IsBannedMember(uint masterID) => _bannedMembers.ContainsKey(masterID);

        /// <summary>
        /// Adds the banned member.
        /// </summary>
        /// <param name="masterID">The master identifier.</param>
        /// <param name="displayName">The display name.</param>
        /// <returns></returns>
        public bool AddBannedMember(uint masterID, string displayName)
        {
            // clan only allow up to 100 banned members
            if (_bannedMembers.Count >= 100)
                return false;
            _bannedMembers.Add(masterID, displayName);
            return true;
        }

        /// <summary>
        /// Removes the banned member.
        /// </summary>
        /// <param name="masterID">The master identifier.</param>
        /// <returns></returns>
        public bool RemoveBannedMember(uint masterID) => _bannedMembers.Remove(masterID);
    }
}
