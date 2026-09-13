using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Store
{
    /// <summary>
    /// Defines a contract for a store that manages the persistence and retrieval of character instances in the game world.
    /// </summary>
    public interface ICharacterStore
    {
        /// <summary>
        /// Captures the characters currently in the store for one synchronous game tick.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel waiting for the store read lock.</param>
        /// <returns>A read-only character view for the current game tick.</returns>
        ValueTask<IReadOnlyDictionary<int, ICharacter>> GetSnapshotAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets the total number of characters in the store.
        /// </summary>
        /// <returns>A <see cref="ValueTask{TResult}"/> that resolves to the character count.</returns>
        ValueTask<int> CountAsync();

        /// <summary>
        /// Asynchronously attempts to add a new character to the store.
        /// </summary>
        /// <param name="character">The character to add.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that resolves to <c>true</c> if the character was added successfully; otherwise, <c>false</c>.</returns>
        ValueTask<bool> AddAsync(ICharacter character);

        /// <summary>
        /// Asynchronously removes a character from the store.
        /// </summary>
        /// <param name="character">The character to remove.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that resolves to <c>true</c> if the character was removed successfully; otherwise, <c>false</c>.</returns>
        ValueTask<bool> RemoveAsync(ICharacter character);

        /// <summary>
        /// Asynchronously finds a character by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the character.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that resolves to the <see cref="ICharacter"/> if found; otherwise, <c>null</c>.</returns>
        ValueTask<ICharacter?> FindByIdAsync(uint id);

        /// <summary>
        /// Asynchronously finds a character by its store index.
        /// </summary>
        ValueTask<ICharacter?> FindByIndexAsync(int index);

        /// <summary>
        /// Finds the currently owned character synchronously from the GameWorker boundary.
        /// </summary>
        ICharacter? FindByMasterId(uint id);

        /// <summary>
        /// Returns whether the store still owns this exact character instance.
        /// </summary>
        bool IsCurrent(ICharacter character);

        /// <summary>
        /// Removes the exact character instance synchronously from the GameWorker boundary.
        /// </summary>
        bool Remove(ICharacter character);

        /// <summary>
        /// Admits and schedules work for an exact currently owned character.
        /// </summary>
        bool TryQueueTask(ICharacter character, ITaskItem task);

    }
}
