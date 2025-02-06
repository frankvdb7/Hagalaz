using System.Threading.Tasks;
using Hagalaz.Data;

namespace Hagalaz.Services.Authorization.Data
{
    public class CharacterUnitOfWork : ICharacterUnitOfWork
    {
        private readonly HagalazDbContext _context;
        private ICharacterOffenceRepository? _characterOffenceRepository;
        
        public ICharacterOffenceRepository CharacterOffenceRepository => _characterOffenceRepository ??= new CharacterOffenceRepository(_context);
        
        public CharacterUnitOfWork(HagalazDbContext context) => _context = context;
        
        public async ValueTask CommitAsync() => await _context.SaveChangesAsync();

        public ValueTask RollbackAsync() => _context.DisposeAsync();
    }
}