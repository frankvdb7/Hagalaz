using System.IO;
using System.Threading.Tasks;

namespace Hagalaz.Services.GameUpdate.Services
{
    public interface ICacheService
    {
        ValueTask<MemoryStream> ReadFileAsync(byte indexId, int fileId);
        bool IsValidFile(byte indexId, int fileId);
    }
}