namespace Hagalaz.Services.JagGrab
{
    /// <summary>
    /// 
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// Gets or sets the local ip.
        /// </summary>
        /// <value>
        /// The local ip.
        /// </value>
        public string LocalIp { get; set; }
        /// <summary>
        /// Gets or sets the local port.
        /// </summary>
        /// <value>
        /// The local port.
        /// </value>
        public int LocalPort { get; set; }
        /// <summary>
        /// Gets or sets the maximum connections.
        /// </summary>
        /// <value>
        /// The maximum connections.
        /// </value>
        public int MaxConnections { get; set; }
    }
}
