using System;
using System.Threading.Tasks;
using Hagalaz.Configuration;
using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using MassTransit;
using Hagalaz.Exceptions;
using Hagalaz.Game.Abstractions.Features.Chat;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Resources;

namespace Hagalaz.Services.GameWorld.Mediator.Consumers
{
    public class ChatSettingsFilterChangedConsumer : IConsumer<ProfileValueChanged<Availability>>
    {
        private readonly IRequestClient<SetContactSettingsRequest> _setContactSettingsRequest;
        private readonly ICharacterContextAccessor _characterContextAccessor;

        public ChatSettingsFilterChangedConsumer(ICharacterContextAccessor characterContextAccessor, IBus publishEndpoint)
        {
            _characterContextAccessor = characterContextAccessor;
            _setContactSettingsRequest = publishEndpoint.CreateRequestClient<SetContactSettingsRequest>();
        }

        public async Task Consume(ConsumeContext<ProfileValueChanged<Availability>> context)
        {
            var character = _characterContextAccessor.Context.Character;
            var message = context.Message;
            if (message.Key == ProfileConstants.ChatSettingsPrivateFilter) 
            {
                character.Session.SendMessage(new SetChatFilterMessage
                {
                    PrivateFilter = message.NewValue
                });
                try
                {
                    var result = await _setContactSettingsRequest.GetResponse<SetContactSettingsResponse>(new SetContactSettingsRequest
                    {
                        MasterId = character.MasterId,
                        Settings = new ContactSettingsDto((ContactAvailability)message.NewValue)
                    });
                }
                catch (NotFoundException)
                {
                    character.SendChatMessage(string.Format(GameStrings.PlayerXDoesNotExist, character.DisplayName));
                }
                catch (Exception)
                {
                    character.SendChatMessage(GameStrings.SomethingWentWrong);
                }
            }
            else if (message.Key == ProfileConstants.ChatSettingsTradeFilter)
            {
                character.Session.SendMessage(new SetChatFilterMessage
                {
                    TradeFilter = message.NewValue
                });
            }
            else if (message.Key == ProfileConstants.ChatSettingsPublicFilter)
            {
                character.Session.SendMessage(new SetChatFilterMessage
                {
                    PublicFilter = message.NewValue
                });
            }
        }
    }
}
