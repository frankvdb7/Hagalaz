using System;

namespace Hagalaz.Services.GameWorld.Configuration.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class GameServerLimits
    {
        /// <summary>
        /// The maximum concurrent connections
        /// </summary>
        private long? _maxConcurrentConnections = 2048;

        /// <summary>
        /// Gets or sets the maximum concurrent connections.
        /// <para>
        /// Defaults to 2048.
        /// </para>
        /// </summary>
        /// <value>
        /// The maximum concurrent connections.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">value - Positive number or null required.</exception>
        public long? MaxConcurrentConnections
        {
            get => _maxConcurrentConnections;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Positive number or null required.");
                }

                _maxConcurrentConnections = value;
            }
        }
    }
}