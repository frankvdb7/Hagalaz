using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Model;
using Hagalaz.Services.GameWorld.Model.Creatures.Characters;
using NSubstitute;
using OpenIddict.Abstractions;
using Raido.Server;
using GameWorldFeatures = Hagalaz.Services.GameWorld.Features;

namespace Hagalaz.Services.GameWorld.Tests;

internal static class AuthenticatedCharacterFeatureSetup
{
    public static ICharacter Create(RaidoHubConnectionContext target, IGameWorldSession session, GameClient gameClient)
    {
        var character = Substitute.For<ICharacter>();
        character.MasterId.Returns(42u);
        character.Session.Returns(session);
        character.Index.Returns(1);
        character.Location.Returns(Location.Create(3200, 3200));
        character.GameClient.Returns(gameClient);
        Set(target, session, character);
        return character;
    }

    public static void Set(RaidoHubConnectionContext target, IGameWorldSession session, ICharacter character)
    {
        target.Features.Set<GameWorldFeatures.ISessionFeature>(new GameWorldFeatures.SessionFeature { Session = session });
        target.Features.Set<GameWorldFeatures.ICharacterFeature>(new GameWorldFeatures.CharacterFeature { Character = character });
        target.Features.Set<GameWorldFeatures.IAuthenticationFeature>(new GameWorldFeatures.AuthenticationFeature
        {
            AuthenticationProperties = new GameWorldFeatures.AuthenticationProperties
            {
                Claims = new Dictionary<string, object> { [OpenIddictConstants.Claims.Subject] = "42" },
                AuthorizationId = "authorization-id"
            }
        });
    }

    public static RaidoHubConnectionStore CreateConnectionStore(RaidoHubConnectionContext target)
    {
        var connections = new RaidoHubConnectionStore();
        connections.Add(target);
        return connections;
    }
}
