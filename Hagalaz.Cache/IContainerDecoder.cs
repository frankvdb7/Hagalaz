using System.IO;

namespace Hagalaz.Cache
{
    public interface IContainerDecoder
    {
        IContainer Decode(MemoryStream stream);
    }
}
