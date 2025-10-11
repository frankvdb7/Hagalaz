namespace Hagalaz.Game.Abstractions.Mediator
{
    /// <summary>
    /// Defines a marker interface for a scoped game mediator. This is typically used to resolve a mediator
    /// instance that is scoped to a specific context, such as a single player's game session.
    /// </summary>
    public interface IScopedGameMediator : IGameMediator
    {
    }
}
