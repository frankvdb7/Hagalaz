using System;

namespace Hagalaz.Services.GameWorld.Configuration.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class GameServerOptions
    {
        /// <summary>
        /// Gets the limits.
        /// </summary>
        /// <value>
        /// The limits.
        /// </value>
        public GameServerLimits Limits { get; } = new GameServerLimits();

        /// <summary>
        /// 
        /// </summary>
        public required int ClientRevision { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public required int ClientRevisionPatch { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public required string AuthenticationToken { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan TickTimeSpan { get; set; } = TimeSpan.FromMilliseconds(600);
    }
}