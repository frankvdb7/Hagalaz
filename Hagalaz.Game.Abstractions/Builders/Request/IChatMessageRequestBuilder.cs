namespace Hagalaz.Game.Abstractions.Builders.Request
{
    /// <summary>
    /// Defines the contract for a chat message request builder, which serves as the entry point
    /// for constructing an <see cref="Model.Creatures.Characters.IChatMessageRequest"/> object using a fluent interface.
    /// </summary>
    public interface IChatMessageRequestBuilder
    {
        /// <summary>
        /// Begins the process of building a new chat message request.
        /// </summary>
        /// <returns>The next step in the fluent builder chain, which requires specifying the source of the request.</returns>
        IChatMessageRequestSource Create();
    }
}