namespace Hagalaz.Services.GameLogon.Services.Model
{
    public record ContactSettingsDto
    {
        public record AvailabilitySettingsDto
        {
            public bool Friends { get; init; }
            public bool Everyone { get; init; }
            public bool Off { get; init; }
        }

        public AvailabilitySettingsDto Availability { get; init; } = default!;
    }
}