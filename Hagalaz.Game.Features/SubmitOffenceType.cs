namespace Hagalaz.Game.Features
{
    /// <summary>
    /// 
    /// </summary>
    public enum SubmitOffenceType : byte
    {
        /// <summary>
        /// The none
        /// </summary>
        None,
        /// <summary>
        /// The kick
        /// </summary>
        Kick = 1,
        /// <summary>
        /// The mute
        /// </summary>
        Mute = 2,
        /// <summary>
        /// The un mute
        /// </summary>
        UnMute = 3,
        /// <summary>
        /// The ban
        /// </summary>
        Ban = 4,
        /// <summary>
        /// The un ban
        /// </summary>
        UnBan = 5,
        /// <summary>
        /// The ip ban
        /// </summary>
        IpBan = 6,
        /// <summary>
        /// The un ip ban
        /// </summary>
        UnIpBan = 7,
        /// <summary>
        /// The ip mute
        /// </summary>
        IpMute = 8,
        /// <summary>
        /// The un ip mute
        /// </summary>
        UnIpMute = 9
    }
}
