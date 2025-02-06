using Hagalaz.Cache.Abstractions.Providers.Model;

namespace Hagalaz.Cache.Abstractions.Providers
{
    public interface IHuffmanCodeProvider
    {
        public HuffmanCoding GetCoding();
    }
}