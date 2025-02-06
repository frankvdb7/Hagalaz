using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Hagalaz.Contacts.Messages;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Raido.Common.Protocol;
using Raido.Server;
using Hagalaz.Exceptions;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Messages.Protocol.Model;
using Hagalaz.Game.Resources;
using Hagalaz.Services.GameWorld.Extensions;

namespace Hagalaz.Services.GameWorld.Hubs
{
    [Authorize]
    public class ContactHub : RaidoHub
    {
        private readonly IRequestClient<AddContactRequest> _addContactRequestClient;
        private readonly IRequestClient<RemoveContactRequest> _removeContactRequestClient;
        private readonly IRequestClient<AddContactMessageRequest> _addContactMessageRequestClient;
        private readonly IMapper _mapper;

        public ContactHub(
            IRequestClient<AddContactRequest> addContactRequestClient, IRequestClient<RemoveContactRequest> removeContactRequestClient,
            IRequestClient<AddContactMessageRequest> addContactMessageRequestClient, IMapper mapper)
        {
            _addContactRequestClient = addContactRequestClient;
            _removeContactRequestClient = removeContactRequestClient;
            _addContactMessageRequestClient = addContactMessageRequestClient;
            _mapper = mapper;
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
            catch (Exception)
            {
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
            try
            {
                var response = await _addContactRequestClient.GetResponse<AddContactResponse>(new AddContactRequest
                {
                    MasterId = masterId.Value, ContactDisplayName = request.DisplayName, Ignore = false
                });
                var message = response.Message;
                var friend = _mapper.Map<Friend>(message.Contact);
                contacts.Friends.Add(friend);

                var friendContact = _mapper.Map<ContactDto>(message.Contact);
                var friendMessage = new FriendsListMessage
                {
                    Friends = new List<ContactDto>
                    {
                        friendContact
                    }
                };
                await Clients.Caller.SendAsync(friendMessage);
            }
            catch (NotFoundException)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = string.Format(GameStrings.PlayerXDoesNotExist, request.DisplayName), Type = ChatMessageType.ChatboxText });
            }
            catch (Exception)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
            }
        }

        [RaidoMessageHandler(typeof(RemoveFriendMessage))]
        public async Task RemoveFriend(RemoveFriendMessage request)
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
                var response = await _removeContactRequestClient.GetResponse<RemoveContactResponse>(new RemoveContactRequest
                {
                    MasterId = masterId.Value, ContactDisplayName = request.DisplayName, Ignore = false
                });
                var message = response.Message;
                contacts.Friends.Remove(message.Contact.MasterId);
            }
            catch (NotFoundException)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = string.Format(GameStrings.PlayerXDoesNotExist, request.DisplayName), Type = ChatMessageType.ChatboxText });
            }
            catch (Exception)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
            }
        }

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
                var response = await _addContactRequestClient.GetResponse<AddContactResponse>(new AddContactRequest
                {
                    MasterId = masterId.Value, ContactDisplayName = request.DisplayName, Ignore = true
                });
                var message = response.Message;
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
            catch (Exception)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
            }
        }

        [RaidoMessageHandler(typeof(RemoveIgnoreMessage))]
        public async Task RemoveIgnore(RemoveIgnoreMessage request)
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
                var response = await _removeContactRequestClient.GetResponse<RemoveContactResponse>(new RemoveContactRequest
                {
                    MasterId = masterId.Value, ContactDisplayName = request.DisplayName, Ignore = true
                });
                var message = response.Message;
                contacts.Ignores.Remove(message.Contact.MasterId);
            }
            catch (NotFoundException)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = string.Format(GameStrings.PlayerXDoesNotExist, request.DisplayName), Type = ChatMessageType.ChatboxText });
            }
            catch (Exception)
            {
                await Clients.Caller.SendAsync(new ChatMessage { Text = GameStrings.SomethingWentWrong, Type = ChatMessageType.ChatboxText });
            }
        }
    }
}