using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Store
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICharacterStore
    {
        /// <summary>
        /// Gets the characters.
        /// </summary>
        /// <returns>
        ///     The characters.
        /// </returns>
        IAsyncEnumerable<ICharacter> FindAllAsync();
        /// <summary>
        /// Count the characters in the repository.
        /// </summary>
        /// <returns></returns>
        ValueTask<int> CountAsync();
        /// <summary>
        /// Tries to add the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        ValueTask<bool> AddAsync(ICharacter character);
        /// <summary>
        /// Removes the specified character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        ValueTask<bool> RemoveAsync(ICharacter character);
        /// <summary>
        /// Finds the specified character async.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        ValueTask<ICharacter?> FindAsync(Func<ICharacter, bool> predicate);
        /// <summary>
        /// Finds the character by id async.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        ValueTask<ICharacter?> FindByIdAsync(uint id);
    }
}