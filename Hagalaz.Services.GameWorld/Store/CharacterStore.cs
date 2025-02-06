using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Services.GameWorld.Configuration.Model;

namespace Hagalaz.Services.GameWorld.Store
{
    public class CharacterStore : ICharacterStore
    {
        /// <summary>
        /// The characters
        /// </summary>
        private readonly ICreatureCollection<ICharacter> _characters;
        private readonly AsyncReaderWriterLock _lock = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterStore" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        public CharacterStore(IOptions<GameServerOptions> options)
        {
            var limitsMaxConcurrentConnections = options.Value.Limits.MaxConcurrentConnections ?? throw new ArgumentNullException(nameof(options));
            _characters = new CreatureCollection<ICharacter>((int)limitsMaxConcurrentConnections);
        }

        /// <summary>
        /// Gets the characters.
        /// </summary>
        /// <returns>
        ///     The characters.
        /// </returns>
        public async IAsyncEnumerable<ICharacter> FindAllAsync()
        {
            using (await _lock.ReaderLockAsync())
            {
                foreach (var character in _characters)
                {
                    yield return character;
                }
            }
        }

        public async ValueTask<int> CountAsync()
        {
            using (await _lock.ReaderLockAsync())
            {
                return _characters.Count;
            }
        }

        /// <summary>
        /// Registers the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        public async ValueTask<bool> AddAsync(ICharacter character)
        {
            using (await _lock.WriterLockAsync())
            {
                return _characters.Add(character);
            }
        }

        /// <summary>
        /// Unregisters the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        public async ValueTask<bool> RemoveAsync(ICharacter character)
        {
            using (await _lock.WriterLockAsync())
            {
                return _characters.Remove(character);
            }
        }

        public ValueTask<ICharacter?> FindAsync(Func<ICharacter, bool> predicate) => FindAllAsync().Where(predicate).FirstOrDefaultAsync();
        public ValueTask<ICharacter?> FindByIdAsync(uint id) => FindAsync(character => character.MasterId == id);
    }
}