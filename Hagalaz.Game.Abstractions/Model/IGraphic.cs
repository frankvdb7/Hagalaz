namespace Hagalaz.Game.Abstractions.Model
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGraphic
    {
        /// <summary>
        /// Gets the graphic id (id from client).
        /// </summary>
        /// <value>The id.</value>
        int Id { get; }
        /// <summary>
        /// Gets the graphic delay.
        /// </summary>
        /// <value>The delay.</value>
        int Delay { get; }
        /// <summary>
        /// Get's the graphic height.
        /// </summary>
        /// <value>The height.</value>
        int Height { get; }
        /// <summary>
        /// Get's the graphic rotation.
        /// </summary>
        /// <value>The rotation.</value>
        int Rotation { get; }
    }
}
