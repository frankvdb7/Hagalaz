using System.IO;

namespace Hagalaz.Cache.Abstractions.Types
{
    public interface IType
    {
        int Id { get; }

        void Decode(MemoryStream stream);

        MemoryStream Encode();
    }
}