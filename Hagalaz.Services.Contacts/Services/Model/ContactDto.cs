using Hagalaz.Contacts.Messages.Model;

namespace Hagalaz.Services.Contacts.Services.Model
{
    public record ContactDto
    {
        public required uint MasterId { get; init; }
        public required string DisplayName { get; init; }
        public string? PreviousDisplayName { get; init; }
        public FriendsChatRank? Rank { get; init; }
        public int? WorldId { get; init; }
        public string? WorldName { get; init; }
        public bool AreMutualFriends { get; init; }
        public ContactSettingsDto? Settings { get; init; }
    }
}