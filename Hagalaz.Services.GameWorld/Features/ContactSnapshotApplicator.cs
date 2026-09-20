using System.Collections.Generic;
using Hagalaz.Contacts.Messages;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Extensions;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Messages.Protocol.Model;
using AutoMapper;

namespace Hagalaz.Services.GameWorld.Features;

internal static class ContactSnapshotApplicator
{
    public static bool TryApply(
        GetContactsResponse response,
        IGameSession session,
        IContactsFeature contacts,
        IMapper mapper)
    {
        if (response.MasterId != session.MasterId ||
            response.SessionGeneration != session.SessionGeneration ||
            response.ConnectionId != session.ConnectionId)
        {
            return false;
        }   

        var friends = new List<Friend>(mapper.Map<IEnumerable<Friend>>(response.Friends) ?? []);
        var ignores = new List<Ignore>(mapper.Map<IEnumerable<Ignore>>(response.Ignores) ?? []);
        var onlineOwners = new List<ContactPresenceOwner>();
        foreach (var contact in response.Friends)
        {
            if (contact.SessionGeneration is { } sessionGeneration &&
                contact.SessionConnectionId is { } connectionId)
            {
                onlineOwners.Add(new ContactPresenceOwner(
                    contact.MasterId,
                    sessionGeneration,
                    connectionId,
                    contact.WorldId,
                    contact.WorldName));
            }
        }

        contacts.ReplaceFriends(friends, onlineOwners, response.ObservationBoundary);
        contacts.Ignores.Set(ignores);

        var friendContacts = new List<ContactDto>(mapper.Map<List<ContactDto>>(response.Friends) ?? []);
        for (var i = 0; i < friendContacts.Count; i++)
        {
            var friendContact = friendContacts[i];
            var friend = contacts.Friends.Get(unchecked((uint)friendContact.MasterId));
            friendContacts[i] = friend is null
                ? friendContact with { WorldId = null, WorldName = null }
                : ReconcileFriendContact(friend, friendContact, contacts.GetPresence(unchecked((uint)friendContact.MasterId)));
        }

        session.SendMessage(new FriendsListMessage { Friends = friendContacts });
        session.SendMessage(new IgnoreListMessage { Ignores = mapper.Map<List<ContactDto>>(response.Ignores) ?? [] });
        return true;
    }

    public static ContactDto ReconcileFriendContact(
        Friend friend,
        ContactDto contact,
        ContactPresenceView presence)
    {
        if (!presence.IsOnline)
        {
            return contact with { WorldId = null, WorldName = null };
        }

        var visibleWorldId = friend.GetWorldId(presence.WorldId);
        return contact with
        {
            WorldId = visibleWorldId,
            WorldName = visibleWorldId is null ? null : presence.WorldName
        };
    }
}
