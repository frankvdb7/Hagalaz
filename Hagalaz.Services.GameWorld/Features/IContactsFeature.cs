using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Services.GameWorld.Features
{
    public interface IContactsFeature
    {
        public IContactList<Friend> Friends { get; }
        public IContactList<Ignore> Ignores { get; }

        Friend? TryApplySignIn(uint masterId, long sessionGeneration, string connectionId);
        Friend? TryApplySignOut(uint masterId, long sessionGeneration, string connectionId);
    }
}
