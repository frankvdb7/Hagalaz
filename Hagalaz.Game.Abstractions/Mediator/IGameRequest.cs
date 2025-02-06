namespace Hagalaz.Game.Abstractions.Mediator
{
    public interface IGameRequest<in TResponse> 
        where TResponse : class
    {
    }
}
