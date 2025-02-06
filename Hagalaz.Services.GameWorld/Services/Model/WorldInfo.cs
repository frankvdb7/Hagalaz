namespace Hagalaz.Services.GameWorld.Services.Model
{
    public record WorldInfo
    {
        public required int Id { get; init; }
        public required WorldLocationInfo Location { get; init; }
        public required WorldSettingsInfo Settings { get; init; }
        public required string Name { get; init; }
        public required string IpAddress { get; init; }
    }
}
