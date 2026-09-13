using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using System.Linq;
using System.Threading;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Services.GameWorld.Configuration.Model;
using Hagalaz.Services.GameWorld.Services;

namespace Hagalaz.Services.GameWorld.Store
{
    public class CharacterStore : ICharacterStore
    {
        /// <summary>
        /// The characters
        /// </summary>
        private readonly ICreatureCollection<ICharacter> _characters;
        private readonly AsyncReaderWriterLock _lock = new();
        private readonly IRsTaskService _taskService;
        private readonly CharacterLogoutState _logoutState;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterStore" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        public CharacterStore(
            IOptions<GameServerOptions> options,
            IRsTaskService taskService,
            CharacterLogoutState logoutState)
        {
            var limitsMaxConcurrentConnections = options.Value.Limits.MaxConcurrentConnections ?? throw new ArgumentNullException(nameof(options));
            _characters = new CreatureCollection<ICharacter>((int)limitsMaxConcurrentConnections);
            _taskService = taskService;
            _logoutState = logoutState;
        }

        /// <summary>
        /// Gets the characters.
        /// </summary>
        /// <returns>
        ///     The characters.
        /// </returns>
        public async ValueTask<IReadOnlyDictionary<int, ICharacter>> GetSnapshotAsync(CancellationToken cancellationToken = default)
        {
            using (await _lock.ReaderLockAsync(cancellationToken))
            {
                return _characters.ToDictionary(character => character.Index);
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
                if (_characters.Any(existing => existing.MasterId == character.MasterId))
                {
                    return false;
                }

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

        public bool Remove(ICharacter character)
        {
            using (_lock.WriterLock())
            {
                return _characters.Remove(character);
            }
        }

        public async ValueTask<ICharacter?> FindByIdAsync(uint id)
        {
            using (await _lock.ReaderLockAsync())
            {
                return _characters.FirstOrDefault(character => character.MasterId == id);
            }
        }

        public ICharacter? FindByMasterId(uint id)
        {
            using (_lock.ReaderLock())
            {
                return _characters.FirstOrDefault(character => character.MasterId == id);
            }
        }

        public bool IsCurrent(ICharacter character)
        {
            using (_lock.ReaderLock())
            {
                var index = character.Index;
                return index >= 1 && index <= _characters.Capacity && ReferenceEquals(_characters[index], character);
            }
        }

        public bool TryQueueTask(ICharacter character, ITaskItem task)
        {
            var admitted = _logoutState.TryAdmit(character, () =>
            {
                using (_lock.ReaderLock())
                {
                    var index = character.Index;
                    if (index < 1 || index > _characters.Capacity || !ReferenceEquals(_characters[index], character))
                    {
                        task.Cancel();
                        return false;
                    }

                    _taskService.Schedule(task);
                    return true;
                }
            });
            if (!admitted)
            {
                task.Cancel();
            }

            return admitted;
        }

        public async ValueTask<ICharacter?> FindByIndexAsync(int index)
        {
            using (await _lock.ReaderLockAsync())
            {
                if (index < 1 || index > _characters.Capacity)
                {
                    return null;
                }

                return _characters[index];
            }
        }
    }
}
