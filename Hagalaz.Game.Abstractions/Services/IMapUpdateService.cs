using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Abstractions.Services
{
    /// <summary>
    /// Defines the service that orchestrates a character's map update.
    /// </summary>
    public interface IMapUpdateService
    {
        /// <summary>
        /// Rebuilds a character's viewport, sends its map packet, and submits visible-region updates.
        /// </summary>
        /// <param name="character">The character whose map should be updated.</param>
        /// <param name="forceUpdate">Whether the client should force the map update.</param>
        /// <param name="renderViewPort">Whether the client should render the viewport after the map update.</param>
        void UpdateMap(ICharacter character, bool forceUpdate, bool renderViewPort = false);
    }
}
