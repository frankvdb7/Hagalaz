using System.IO;

namespace Hagalaz.Cache
{
    public interface IContainerFactory
    {
        IContainer Decode(MemoryStream stream);
    }
}
