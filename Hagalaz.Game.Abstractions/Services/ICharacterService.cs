using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that manages player characters in the game world.
    /// </summary>
    public interface ICharacterService
    {
        /// <summary>
        /// Finds a character by their master account ID.
        /// </summary>
        /// <param name="masterId">The master account ID of the character to find.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation. The task result contains the <see cref="ICharacter"/> if found; otherwise, <c>null</c>.</returns>
        public ValueTask<ICharacter?> FindByMasterId(uint masterId);

        /// <summary>
        /// Finds a character by their server index.
        /// </summary>
        /// <param name="index">The server index of the character to find.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation. The task result contains the <see cref="ICharacter"/> if found; otherwise, <c>null</c>.</returns>
        public ValueTask<ICharacter?> FindByIndex(int index);

        /// <summary>
        /// Retrieves all characters currently in the game world.
        /// </summary>
        /// <returns>An <see cref="IAsyncEnumerable{T}"/> that contains all characters.</returns>
        public IAsyncEnumerable<ICharacter> FindAll();

        /// <summary>
        /// Adds a character to the game world.
        /// </summary>
        /// <param name="character">The character to add.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation. The task result contains <c>true</c> if the character was added successfully; otherwise, <c>false</c>.</returns>
        public ValueTask<bool> AddAsync(ICharacter character);

        /// <summary>
        /// Removes a character from the game world.
        /// </summary>
        /// <param name="character">The character to remove.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation. The task result contains <c>true</c> if the character was removed successfully; otherwise, <c>false</c>.</returns>
        public ValueTask<bool> RemoveAsync(ICharacter character);

        /// <summary>
        /// Gets the total number of characters currently in the game world.
        /// </summary>
        /// <returns>A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation. The task result contains the total number of characters.</returns>
        public ValueTask<int> CountAsync();
    }
}