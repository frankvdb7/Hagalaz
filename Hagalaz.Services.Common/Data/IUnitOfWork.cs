using System.Threading.Tasks;

namespace Hagalaz.Services.Common.Data
{
    public interface IUnitOfWork
    {
        ValueTask CommitAsync();
        ValueTask RollbackAsync();
    }
}