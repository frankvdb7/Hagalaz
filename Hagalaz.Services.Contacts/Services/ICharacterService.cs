using Hagalaz.Services.Contacts.Services.Model;

namespace Hagalaz.Services.Contacts.Services
{
    public interface ICharacterService
    {
        ValueTask<CharacterDto?> FindCharacterByIdAsync(uint id);
        ValueTask<CharacterDto?> FindCharacterByDisplayName(string name);
    }
}
