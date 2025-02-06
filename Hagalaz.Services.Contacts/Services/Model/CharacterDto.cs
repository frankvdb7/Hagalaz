namespace Hagalaz.Services.Contacts.Services.Model
{
    public record CharacterDto
    {
        public record ClaimDto
        {
            public required string Name { get; init; }
        }
        public required uint MasterId { get; init; }
        public required string DisplayName { get; init; }
        public string? PreviousDisplayName { get; init; }
        public IReadOnlyCollection<ClaimDto>? Claims { get; init; }
    }
}