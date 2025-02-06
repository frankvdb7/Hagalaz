using Hagalaz.Contacts.Messages;
using Hagalaz.Contacts.Messages.Model;
using Hagalaz.Services.Contacts.Services;
using MassTransit;

namespace Hagalaz.Services.Contacts.Consumers
{
    public class GetContactsConsumer : IConsumer<GetContactsRequest>
    {
        private readonly IContactService _contactService;

        public GetContactsConsumer(IContactService contactService)
        {
            _contactService = contactService;
        }

        public async Task Consume(ConsumeContext<GetContactsRequest> context)
        {
            var message = context.Message;
            var friends = await _contactService.FindFriendsByIdAsync(message.MasterId);
            var ignores = await _contactService.FindIgnoresByIdAsync(message.MasterId);

            await context.RespondAsync(new GetContactsResponse()
            {
                MasterId = message.MasterId,
                Friends = friends.Select(c => new ContactDto()
                {
                    MasterId = c.MasterId,
                    DisplayName = c.DisplayName,
                    PreviousDisplayName = c.PreviousDisplayName,
                    Rank = c.Rank,
                    WorldId = c.WorldId,
                    WorldName = c.WorldName,
                    AreMutualFriends = c.AreMutualFriends,
                    Settings = new ContactSettingsDto(c.Settings?.Availability.Off == true ? ContactAvailability.Off :
                                c.Settings?.Availability.Friends == true ? ContactAvailability.Friends : ContactAvailability.Everyone)
                })
                    .ToList(),
                Ignores = ignores.Select(c => new ContactDto()
                {
                    MasterId = c.MasterId,
                    DisplayName = c.DisplayName,
                    PreviousDisplayName = c.PreviousDisplayName
                })
                    .ToList()
            });
        }
    }
}
