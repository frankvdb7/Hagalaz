using Raido.Server;
using OpenIddict.Abstractions;
using System;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Features;

namespace Hagalaz.Services.GameWorld.Extensions
{
    public static class RaidoCallerContextExtensions
    {
        public static IAuthenticationFeature GetAuthentication(this RaidoCallerContext context) => context.Features.Get<IAuthenticationFeature>()!;
        public static uint? GetMasterId(this RaidoCallerContext context)
        {
            if (context.GetAuthentication()?.AuthenticationProperties?.TryGetClaim<string>(OpenIddictConstants.Claims.Subject, out var masterId) == true)
            {
                return Convert.ToUInt32(masterId);
            }
            return null;
        }
        public static IContactsFeature GetContacts(this RaidoCallerContext context) => context.Features.Get<IContactsFeature>()!;
        public static IGameSession GetSession(this RaidoCallerContext context) => context.Features.Get<ISessionFeature>()?.Session!;
        public static ICharacter GetCharacter(this RaidoCallerContext context) => context.Features.Get<ICharacterFeature>()?.Character!;
        public static IUserProfileFeature GetUserProfile(this RaidoCallerContext context) => context.Features.Get<IUserProfileFeature>()!;
    }
}
