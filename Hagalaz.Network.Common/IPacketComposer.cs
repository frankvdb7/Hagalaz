namespace Hagalaz.Network.Common
{
    /// <summary>
    /// 
    /// </summary>
    public interface IPacketComposer
    {
        /// <summary>
        /// Serializes this instance.
        /// </summary>
        /// <returns></returns>
        byte[] Serialize();
    }
}
