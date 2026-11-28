using System.Buffers;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Raido.Common.Protocol;
using Raido.Server;
using Hagalaz.Exceptions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Messages.Protocol.Model;
using Hagalaz.Services.GameWorld.Extensions;
using Hagalaz.Services.GameWorld.Features;

namespace Hagalaz.Services.GameWorld.Hubs
{
    [Authorize]
    public class ContactHub : RaidoHub
    {
        private readonly IRequestClient<AddContactRequest> _addContactRequestClient;
        private readonly IRequestClient<RemoveContactRequest> _removeContactRequestClient;
        private readonly IRequestClient<AddContactMessageRequest> _addContactMessageRequestClient;
        private readonly IMapper _mapper;
        private readonly ILogger<ContactHub> _logger;

        public ContactHub(
            IRequestClient<AddContactRequest> addContactRequestClient, IRequestClient<RemoveContactRequest> removeContactRequestClient,
            IRequestClient<AddContactMessageRequest> addContactMessageRequestClient, IMapper mapper, ILogger<ContactHub> logger)
        {
            _addContactRequestClient = addContactRequestClient;
            _removeContactRequestClient = removeContactRequestClient;
            _addContactMessageRequestClient = addContactMessageRequestClient;
            _mapper = mapper;
            _logger = logger;
        }

        [RaidoMessageHandler(typeof(AddContactMessage))]
        public async Task AddMessage(AddContactMessage addContactMessage)
        {
            var character = Context.GetCharacter();
            try
            {
                await _addContactMessageRequestClient.GetResponse<AddContactMessageResponse>(new AddContactMessageRequest()
                {
                    MasterId = character.MasterId,
                    ContactDisplayName = addContactMessage.ContactDisplayName,
                    MessageLength = addContactMessage.MessageLength,
                    MessagePayload = addContactMessage.MessagePayload.ToArray()
                });
                character.Session.SendMessage(new AddContactSenderMessage()
                {
                    ContactDisplayName = addContactMessage.ContactDisplayName,
                    MessageLength = addContactMessage.MessageLength,
                    MessagePayload = addContactMessage.MessagePayload
                });
            }
            catch (NotFoundException)
            {
                character.SendChatMessage(string.Format(GameStrings.PlayerXDoesNotExist, addContactMessage.ContactDisplayName));
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogWarning(ex, "ContactHub {Operation} request timed out.", "AddContactMessage");
                character.SendChatMessage(GameStrings.SomethingWentWrong);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ContactHub {Operation} request failed.", "AddContactMessage");
                character.SendChatMessage(GameStrings.SomethingWentWrong);
            }
        }

        [RaidoMessageHandler(typeof(AddFriendMessage))]
        public async Task AddFriend(AddFriendMessage request)
        {
            var masterId = Context.GetMasterId();
            if (masterId == null)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
                return;
            }
            var contacts = Context.GetContacts();
            await contacts.WaitForInitialSnapshotAsync(Context.ConnectionAborted);
            var observationBoundary = contacts.BeginObservationWindow();
            try
            {
                var response = await _addContactRequestClient.GetResponse<AddContactResponse, ContactMutationFailedResponse>(new AddContactRequest
                {
                    MasterId = masterId.Value, ContactDisplayName = request.DisplayName, Ignore = false
                });
                if (!response.Is(out Response<AddContactResponse>? success))
                {
                    await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
                    return;
                }
                var message = success.Message;
                var friend = _mapper.Map<Friend>(message.Contact);
                var onlineOwner = message.Contact.SessionGeneration is { } sessionGeneration
                    && message.Contact.SessionConnectionId is { } connectionId
                    ? new ContactPresenceOwner(
                        message.Contact.MasterId,
                        sessionGeneration,
                        connectionId,
                        message.Contact.WorldId,
                        message.Contact.WorldName)
                    : null;
                contacts.AddFriend(friend, onlineOwner, observationBoundary);

                var friendContact = _mapper.Map<ContactDto>(message.Contact);
                var friendMessage = new FriendsListMessage
                {
                    Friends = new List<ContactDto>
                    {
                        ContactSnapshotApplicator.ReconcileFriendContact(
                            friend,
                            friendContact,
                            contacts.GetPresence(message.Contact.MasterId))
                    }
                };
                await Clients.Caller.SendAsync(friendMessage);
            }
            catch (NotFoundException)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = string.Format(GameStrings.PlayerXDoesNotExist, request.DisplayName), Type = ChatMessageType.ChatboxText });
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogWarning(ex, "ContactHub {Operation} request timed out.", "AddFriend");
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ContactHub {Operation} request failed.", "AddFriend");
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
            }
            finally
            {
                contacts.EndObservationWindow();
            }
        }

        [RaidoMessageHandler(typeof(RemoveFriendMessage))]
        public Task RemoveFriend(RemoveFriendMessage request) =>
            RemoveContact(request.DisplayName, ignore: false, (contacts, contactId) => contacts.RemoveFriend(contactId));

        [RaidoMessageHandler(typeof(AddIgnoreMessage))]
        public async Task AddIgnore(AddIgnoreMessage request)
        {
            var masterId = Context.GetMasterId();
            if (masterId == null)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
                return;
            }
            var contacts = Context.GetContacts();
            try
            {
                await contacts.WaitForInitialSnapshotAsync(Context.ConnectionAborted);
                var response = await _addContactRequestClient.GetResponse<AddContactResponse, ContactMutationFailedResponse>(new AddContactRequest
                {
                    MasterId = masterId.Value, ContactDisplayName = request.DisplayName, Ignore = true
                });
                if (!response.Is(out Response<AddContactResponse>? success))
                {
                    await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
                    return;
                }
                var message = success.Message;
                var ignore = _mapper.Map<Ignore>(message.Contact);
                contacts.Ignores.Add(ignore);

                var ignoreContact = _mapper.Map<ContactDto>(message.Contact);
                var ignoreMessage = new IgnoreListMessage
                {
                    Ignores = new List<ContactDto>
                    {
                        ignoreContact
                    }
                };
                await Clients.Caller.SendAsync(ignoreMessage);
            }
            catch (NotFoundException)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = string.Format(GameStrings.PlayerXDoesNotExist, request.DisplayName), Type = ChatMessageType.ChatboxText });
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogWarning(ex, "ContactHub {Operation} request timed out.", "AddIgnore");
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ContactHub {Operation} request failed.", "AddIgnore");
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
            }
        }

        [RaidoMessageHandler(typeof(RemoveIgnoreMessage))]
        public Task RemoveIgnore(RemoveIgnoreMessage request) =>
            RemoveContact(request.DisplayName, ignore: true, (contacts, contactId) => contacts.Ignores.Remove(contactId));

        private async Task RemoveContact(string displayName, bool ignore, Action<IContactsFeature, uint> removeLocal)
        {
            var masterId = Context.GetMasterId();
            if (masterId == null)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
                return;
            }
            var contacts = Context.GetContacts();
            try
            {
                await contacts.WaitForInitialSnapshotAsync(Context.ConnectionAborted);
                var response = await _removeContactRequestClient.GetResponse<RemoveContactResponse, ContactMutationFailedResponse>(new RemoveContactRequest
                {
                    MasterId = masterId.Value, ContactDisplayName = displayName, Ignore = ignore
                });
                if (!response.Is(out Response<RemoveContactResponse>? success))
                {
                    await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
                    return;
                }
                removeLocal(contacts, success.Message.Contact.MasterId);
            }
            catch (NotFoundException)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = string.Format(GameStrings.PlayerXDoesNotExist, displayName), Type = ChatMessageType.ChatboxText });
            }
            catch (RequestTimeoutException ex)
            {
                _logger.LogWarning(ex, "ContactHub {Operation} request timed out.", ignore ? "RemoveIgnore" : "RemoveFriend");
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ContactHub {Operation} request failed.", ignore ? "RemoveIgnore" : "RemoveFriend");
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
            }
        }
    }
}
