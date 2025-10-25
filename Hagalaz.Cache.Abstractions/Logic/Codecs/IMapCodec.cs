using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Abstractions.Logic.Codecs
{
    /// <summary>
    /// Defines a contract for a codec that handles the serialization and deserialization of map data.
    /// </summary>
    public interface IMapCodec : ITypeCodec<IMapType>
    {
    }
}
