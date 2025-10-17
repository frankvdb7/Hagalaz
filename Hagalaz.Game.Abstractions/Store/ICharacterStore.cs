using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Store
{
    /// <summary>
    /// Defines a contract for a store that manages the persistence and retrieval of character instances in the game world.
    /// </summary>
    public interface ICharacterStore
    {
        /// <summary>
        /// Asynchronously retrieves all characters currently in the store.
        /// </summary>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> of <see cref="ICharacter"/> instances.</returns>
        IAsyncEnumerable<ICharacter> FindAllAsync();

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
        /// Asynchronously finds a character that matches the specified predicate.
        /// </summary>
        /// <param name="predicate">The condition to test each character against.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that resolves to the first matching <see cref="ICharacter"/>, or <c>null</c> if no character is found.</returns>
        ValueTask<ICharacter?> FindAsync(Func<ICharacter, bool> predicate);

        /// <summary>
        /// Asynchronously finds a character by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the character.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that resolves to the <see cref="ICharacter"/> if found; otherwise, <c>null</c>.</returns>
        ValueTask<ICharacter?> FindByIdAsync(uint id);
    }
}