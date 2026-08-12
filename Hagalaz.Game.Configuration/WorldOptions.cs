using System;

namespace Hagalaz.Game.Configuration
{
    public class WorldOptions
    {
        public const string Key = "World";

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ListenHost { get; set; } = "127.0.0.1";
        public WorldEndpointOptions AdvertisedEndpoint { get; set; } = new();
        public TimeSpan RegistrationLeaseDuration { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan RegistrationRenewalInterval { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan RegistrationRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
        public int SpawnPointX { get; set; }
        public int SpawnPointY { get; set; }
        public int SpawnPointZ { get; set; }
        public string WelcomeMessage { get; set; } = string.Empty;
        public string MessageOfTheWeek { get; set; } = string.Empty;
    }

    public sealed class WorldEndpointOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
    }
}
