namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICreatureRenderInformation
    {
        /// <summary>
        /// Contains current animation.
        /// </summary>
        /// <value>The current animation.</value>
        IAnimation CurrentAnimation { get; }
        /// <summary>
        /// Gets a value indicating whether [flag update required].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [flag update required]; otherwise, <c>false</c>.
        /// </value>
        bool FlagUpdateRequired { get; }
        /// <summary>
        /// Gets the last location.
        /// </summary>
        /// <value>
        /// The last location.
        /// </value>
        ILocation LastLocation { get; }
        /// <summary>
        /// Gets the currently active graphics.
        /// </summary>
        /// <returns>Returns a <code>Graphic</code> objects.</returns>
        IGraphic GetCurrentGraphics(int id);
        /// <summary>
        /// Ticks this instance.
        /// </summary>
        void Tick();
        /// <summary>
        /// Resets this instance.
        /// </summary>
        void Reset();
        /// <summary>
        /// Initializes rendering information.
        /// </summary>
        void OnRegistered();
    }
}
