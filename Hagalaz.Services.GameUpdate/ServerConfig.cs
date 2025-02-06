namespace Hagalaz.Services.GameUpdate
{
    public record ServerConfig
    {
        public const string Key = "Configuration";
        public int ClientRevision { get; init; }
        public int ClientRevisionPatch { get; init; }
        public string ServerToken { get; init; } = default!;
        public int[] UpdateKeys { get; init; } = default!;
    }
}
