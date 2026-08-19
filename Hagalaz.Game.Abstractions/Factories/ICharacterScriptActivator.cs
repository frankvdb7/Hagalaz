using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Factories
{
    /// <summary>
    /// Creates character scripts from the active character scope.
    /// </summary>
    public interface ICharacterScriptActivator
    {
        /// <summary>
        /// Creates a registered character script.
        /// </summary>
        /// <typeparam name="TScript">The character script type.</typeparam>
        /// <returns>The created script.</returns>
        TScript Create<TScript>() where TScript : class, ICharacterScript;
    }
}
