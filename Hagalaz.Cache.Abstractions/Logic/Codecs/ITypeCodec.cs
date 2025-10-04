using System.IO;
using Hagalaz.Cache.Abstractions.Types;

namespace Hagalaz.Cache.Abstractions.Logic.Codecs
{
    public interface ITypeCodec<T> where T : IType
    {
        T Decode(int id, MemoryStream stream);
        MemoryStream Encode(T instance);
    }
}