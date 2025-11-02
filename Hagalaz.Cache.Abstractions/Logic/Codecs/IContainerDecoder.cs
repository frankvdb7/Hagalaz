using System.IO;
using Hagalaz.Cache.Abstractions.Model;

namespace Hagalaz.Cache.Abstractions.Logic.Codecs
{
    public interface IContainerDecoder
    {
        IContainer Decode(MemoryStream stream);
    }
}
