using System.IO;

namespace Hagalaz.Cache.Abstractions.Types
{
    /// <summary>
    /// 
    /// </summary>
    public interface IType
    {
        /// <summary>
        /// Decodes the specified buffer.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        void Decode(MemoryStream buffer);

        /// <summary>
        /// Encodes this instance.
        /// </summary>
        /// <returns></returns>
        MemoryStream Encode();
    }
}
