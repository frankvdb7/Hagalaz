namespace Hagalaz.Services.Contacts.Services.Model
{
    public record ContactSettingsDto
    {
        public record AvailabilitySettingsDto
        {
            public required bool Friends { get; init; }
            public required bool Everyone { get; init; }
            public required bool Off { get; init; }
        }

        public required AvailabilitySettingsDto Availability { get; init; }
    }
}