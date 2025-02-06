using System.Threading.Tasks;
using Hagalaz.Data;

#pragma warning disable 649

namespace Hagalaz.Services.GameLogon.Data
{
    public class CharacterUnitOfWork : ICharacterUnitOfWork
    {
        private readonly HagalazDbContext _context;

        private readonly ICharacterPreferencesRepository? _characterPreferencesRepository;
        private readonly ICharacterRepository? _characterStore;
        private readonly ICharacterContactsRepository? _characterContactsRepository;
        private readonly ICharacterPermissionsRepository? _characterPermissionsRepository;
        private readonly ICharacterOffenceRepository? _characterOffenceRepository;

        public ICharacterRepository characterStore => _characterStore ?? new CharacterRepository(_context);

        public ICharacterPreferencesRepository CharacterPreferencesRepository =>
            _characterPreferencesRepository ?? new CharacterPreferencesRepository(_context);

        public ICharacterContactsRepository CharacterContactsRepository => _characterContactsRepository ?? new CharacterContactsRepository(_context);

        public ICharacterPermissionsRepository CharacterPermissionsRepository =>
            _characterPermissionsRepository ?? new CharacterPermissionsRepository(_context);

        public ICharacterOffenceRepository CharacterOffenceRepository => _characterOffenceRepository ?? new CharacterOffenceRepository(_context);

        public CharacterUnitOfWork(HagalazDbContext context) => _context = context;

        public async ValueTask CommitAsync() => await _context.SaveChangesAsync();

        public ValueTask RollbackAsync() => _context.DisposeAsync();
    }
}