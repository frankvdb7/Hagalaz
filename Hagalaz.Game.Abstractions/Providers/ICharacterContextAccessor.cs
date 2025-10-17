using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Providers
{
    /// <summary>
    /// Defines a contract for accessing the current character's context, typically within a scoped request or operation.
    /// This allows services to retrieve information about the player who initiated an action.
    /// </summary>
    public interface ICharacterContextAccessor
    {
        /// <summary>
        /// Gets the context of the current character.
        /// </summary>
        ICharacterContext Context { get; }
    }
}
