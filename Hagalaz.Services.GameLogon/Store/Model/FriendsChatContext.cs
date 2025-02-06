using System.Collections.Generic;

namespace Hagalaz.Services.GameLogon.Store.Model
{
    public class FriendsChatContext
    {
        private readonly HashSet<uint> _members = new();

        public IReadOnlyCollection<uint> Members => _members;
        
        public string Name { get; }
        
        public uint OwnerId { get; }

        public FriendsChatContext(string name, uint ownerId)
        {
            Name = name;
            OwnerId = ownerId;
        }

        public void AddMember(uint characterId) => _members.Add(characterId);

        public bool RemoveMember(uint characterId) => _members.Remove(characterId);
    }
}