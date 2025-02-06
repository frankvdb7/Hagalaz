using Hagalaz.Data.Entities;

namespace Hagalaz.Services.Contacts.Data
{
    public interface ICharacterRepository
    {
        IQueryable<Character> FindByIdAsync(uint id);
        IQueryable<Character> FindByDisplayNameAsync(string displayName);
    }
}