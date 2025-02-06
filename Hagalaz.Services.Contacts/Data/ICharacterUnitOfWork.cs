using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.Contacts.Data
{
    public interface ICharacterUnitOfWork : IUnitOfWork
    {
        ICharacterRepository CharacterRepository { get; }
        ICharacterProfilesRepository CharacterProfilesRepository { get; }
        ICharacterContactsRepository CharacterContactsRepository { get; }
        ICharacterPermissionsRepository CharacterPermissionsRepository { get; }
    }
}