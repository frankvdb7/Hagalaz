namespace Hagalaz.Game.Abstractions.Model.Creatures
{
    /// <summary>
    /// Defines the contract for an object that holds the rendering information for a creature,
    /// such as its current animation, location, and whether it needs a visual update.
    /// </summary>
    public interface ICreatureRenderInformation
    {
        /// <summary>
        /// Gets the current animation being performed by the creature.
        /// </summary>
        IAnimation CurrentAnimation { get; }

        /// <summary>
        /// Gets a value indicating whether a visual update (e.g., for appearance changes) is required for the creature.
        /// </summary>
        bool FlagUpdateRequired { get; }

        /// <summary>
        /// Gets the location of the creature in the previous game tick.
        /// </summary>
        ILocation LastLocation { get; }

        /// <summary>
        /// Gets the currently active graphical effect for a given ID.
        /// </summary>
        /// <param name="id">The identifier of the graphic to retrieve.</param>
        /// <returns>The <see cref="IGraphic"/> object if active; otherwise, <c>null</c>.</returns>
        IGraphic GetCurrentGraphics(int id);

        /// <summary>
        /// Processes a single tick for the rendering information, updating animations and other time-sensitive data.
        /// </summary>
        void Tick();

        /// <summary>
        /// Resets the rendering information to a default state.
        /// </summary>
        void Reset();

        /// <summary>
        /// Initializes the rendering information when the creature is first registered with the world.
        /// </summary>
        void OnRegistered();
    }
}
