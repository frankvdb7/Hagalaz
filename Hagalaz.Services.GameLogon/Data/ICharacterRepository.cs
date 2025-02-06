using System.Linq;
using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameLogon.Data
{
    public interface ICharacterRepository
    {
        IQueryable<Character> FindByIdAsync(uint id);
        IQueryable<Character> FindByDisplayNameAsync(string displayName);
    }
}