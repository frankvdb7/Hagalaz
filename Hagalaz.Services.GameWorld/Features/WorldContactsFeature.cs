using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    public class WorldContactsFeature : IContactsFeature
    {
        private readonly ICharacter _character;

        public WorldContactsFeature(ICharacter character) => _character = character;

        public IContactList<Friend> Friends => _character.Friends;

        public IContactList<Ignore> Ignores => _character.Ignores;
    }
}
