using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.GameLogon.Data
{
    public interface ICharacterUnitOfWork : IUnitOfWork
    {
        ICharacterRepository characterStore { get; }
        ICharacterPreferencesRepository CharacterPreferencesRepository { get; }
        ICharacterContactsRepository CharacterContactsRepository { get; }
        ICharacterPermissionsRepository CharacterPermissionsRepository { get; }
        ICharacterOffenceRepository CharacterOffenceRepository { get; }
    }
}