using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    public class LobbyContactsFeature : IContactsFeature
    {
        public IContactList<Friend> Friends => new ContactList<Friend>();

        public IContactList<Ignore> Ignores => new ContactList<Ignore>();
    }
}
