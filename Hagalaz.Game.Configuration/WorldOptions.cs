namespace Hagalaz.Game.Configuration
{
    /// <summary>
    /// 
    /// </summary>
    public class WorldOptions
    {
        public const string Key = "World";
        
        /// <summary>
        /// Gets or sets the world identifier.
        /// </summary>
        /// <value>
        /// The world identifier.
        /// </value>
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public int SpawnPointX { get; set; }
        public int SpawnPointY { get; set; }
        public int SpawnPointZ { get; set; }
        public string WelcomeMessage { get; set; } = default!;
        public string MessageOfTheWeek { get; set; } = default!;
    }
}