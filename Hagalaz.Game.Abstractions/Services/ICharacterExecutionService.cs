using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Abstractions.Services;

/// <summary>
/// Admits deferred gameplay work for an exact character owned by the GameWorld.
/// </summary>
public interface ICharacterExecutionService
{
    /// <summary>
    /// Queues work only when the expected character is still the exact current owner
    /// and has not entered logout.
    /// </summary>
    IRsTaskHandle Queue(ICharacter expectedCharacter, ITaskItem task);

    /// <summary>
    /// Queues result-producing work only when the expected character is still the
    /// exact current owner and has not entered logout.
    /// </summary>
    IRsTaskHandle<TResult> Queue<TResult>(ICharacter expectedCharacter, ITaskItem<TResult> task);

    /// <summary>
    /// Revokes remaining work admitted for the exact character.
    /// </summary>
    void Revoke(ICharacter expectedCharacter);
}
