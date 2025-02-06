using System.Threading.Tasks;
using Hagalaz.Data;

namespace Hagalaz.Services.Characters.Data
{
    public class CharacterUnitOfWork : ICharacterUnitOfWork
    {
        private readonly HagalazDbContext _context;
        private ICharacterRepository? _characterRepository;
        private ICharacterStatisticsRepository? _characterStatisticsRepository;
        private ICharacterItemRepository? _characterItemRepository;
        private ICharacterLookRepository? _characterLookRepository;
        private ICharacterFamiliarRepository? _characterFamiliarRepository;
        private ICharacterMusicRepository? _characterMusicRepository;
        private ICharacterMusicPlaylistRepository? _characterMusicPlaylistRepository;
        private ICharacterFarmingRepository? _characterFarmingRepository;
        private ICharacterSlayerRepository? _characterSlayerRepository;
        private ICharacterNotesRepository? _characterNotesRepository;
        private ICharacterProfileRepository? _characterProfileRepository;
        private ICharacterItemLookRepository? _characterItemLookRepository;
        private ICharacterStateRepository? _characterStateRepository;

        public ICharacterRepository CharacterRepository => _characterRepository ??= new CharacterRepository(_context);
        public ICharacterStatisticsRepository CharacterStatisticsRepository => _characterStatisticsRepository ??= new CharacterStatisticsRepository(_context);
        public ICharacterItemRepository CharacterItemRepository => _characterItemRepository ??= new CharacterItemRepository(_context);
        public ICharacterLookRepository CharacterLookRepository => _characterLookRepository ??= new CharacterLookRepository(_context);
        public ICharacterFamiliarRepository CharacterFamiliarRepository => _characterFamiliarRepository ??= new CharacterFamiliarRepository(_context);
        public ICharacterMusicRepository CharacterMusicRepository => _characterMusicRepository ??= new CharacterMusicRepository(_context);
        public ICharacterMusicPlaylistRepository CharacterMusicPlaylistRepository => _characterMusicPlaylistRepository ??= new CharacterMusicPlaylistRepository(_context);
        public ICharacterFarmingRepository CharacterFarmingRepository => _characterFarmingRepository ??= new CharacterFarmingRepository(_context);
        public ICharacterSlayerRepository CharacterSlayerRepository => _characterSlayerRepository ??= new CharacterSlayerRepository(_context);
        public ICharacterNotesRepository CharacterNotesRepository => _characterNotesRepository ??= new CharacterNotesRepository(_context);
        public ICharacterProfileRepository CharacterProfileRepository => _characterProfileRepository ??= new CharacterProfileRepository(_context);
        public ICharacterItemLookRepository CharacterItemLookRepository => _characterItemLookRepository ??= new CharacterItemLookRepository(_context);
        public ICharacterStateRepository CharacterStateRepository => _characterStateRepository ??= new CharacterStateRepository(_context);

        public CharacterUnitOfWork(HagalazDbContext context) => _context = context;
        public async ValueTask CommitAsync() => await _context.SaveChangesAsync();
        public ValueTask RollbackAsync() => _context.DisposeAsync();
    }
}