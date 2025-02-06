using Raido.Common.Protocol;

namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHintIcon
    {
        /// <summary>
        /// Contains the index.
        /// </summary>
        int Index { get; set; }
        /// <summary>
        /// Converts this instance to a raido message.
        /// </summary>
        /// <returns></returns>
        RaidoMessage ToMessage();
    }
}
