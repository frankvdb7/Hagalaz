using System.Threading.Tasks;

namespace Hagalaz.Services.Authorization.Services
{
    public interface IOffenceService
    {
        ValueTask<bool> HasActiveBanOffenceAsync(uint id);
    }
}