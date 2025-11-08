using System.Threading.Tasks;
using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Messages.Protocol;
using Microsoft.AspNetCore.Authorization;
using Raido.Common.Protocol;
using Raido.Server;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Hubs
{
    [Authorize]
    public class ProfileHub : RaidoHub
    {
        private readonly IScopedGameMediator _gameMediator;

        public ProfileHub(IScopedGameMediator gameMediator)
        {
            _gameMediator = gameMediator;
        }

        [RaidoMessageHandler(typeof(SetChatFilterMessage))]
        public Task SetChatFilter(SetChatFilterMessage message)
        {
            _gameMediator.Publish(new ProfileSetEnumAction(ProfileConstants.ChatSettingsPublicFilter, message.PublicFilter ?? Availability.Everyone));
            _gameMediator.Publish(new ProfileSetEnumAction(ProfileConstants.ChatSettingsPrivateFilter, message.PrivateFilter ?? Availability.Everyone));
            _gameMediator.Publish(new ProfileSetEnumAction(ProfileConstants.ChatSettingsTradeFilter, message.TradeFilter ?? Availability.Everyone));
            return Task.CompletedTask;
        }

        [RaidoMessageHandler(typeof(SetClientWindowMessage))]
        public void SetClientWindow(SetClientWindowMessage message)
        {
            var character = Context.GetCharacter();
            character.GameClient.DisplayMode = message.Mode;
            character.GameClient.ScreenSizeX = message.SizeX;
            character.GameClient.ScreenSizeY = message.SizeY;
            character.EventManager.SendEvent(new ScreenChangedEvent(character, message.Mode, message.SizeX, message.SizeY));
        }
    }
}
