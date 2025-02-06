using Hagalaz.Data;

#pragma warning disable 649

namespace Hagalaz.Services.Contacts.Data
{
    public class CharacterUnitOfWork : ICharacterUnitOfWork
    {
        private readonly HagalazDbContext _context;

        private ICharacterContactsRepository? _characterContactsRepository;
        private ICharacterRepository? _characterRepository;
        private ICharacterPermissionsRepository? _characterPermissionsRepository;
        private ICharacterProfilesRepository? _characterProfilesRepository;

        public ICharacterRepository CharacterRepository => _characterRepository ??= new CharacterRepository(_context);
        public ICharacterContactsRepository CharacterContactsRepository => _characterContactsRepository ??= new CharacterContactsRepository(_context);
        public ICharacterPermissionsRepository CharacterPermissionsRepository => _characterPermissionsRepository ??= new CharacterPermissionsRepository(_context);
        public ICharacterProfilesRepository CharacterProfilesRepository => _characterProfilesRepository ??= new CharacterProfilesRepository(_context);

        public CharacterUnitOfWork(HagalazDbContext context) => _context = context;

        public async ValueTask CommitAsync() => await _context.SaveChangesAsync();

        public ValueTask RollbackAsync() => _context.DisposeAsync();
    }
}