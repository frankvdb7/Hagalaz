namespace Hagalaz.Game.Abstractions.Mediator
{
    /// <summary>
    /// Defines a marker interface for a request message that expects a response of a specific type.
    /// </summary>
    /// <typeparam name="TResponse">The type of the expected response message.</typeparam>
    public interface IGameRequest<in TResponse> 
        where TResponse : class
    {
    }
}
