using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Sound;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="ISound"/> interface to simplify playing sounds in the game world.
    /// </summary>
    public static class SoundExtensions
    {
        /// <summary>
        /// Plays a sound to all characters within a specified distance of a source creature.
        /// </summary>
        /// <param name="sound">The sound to be played.</param>
        /// <param name="source">The creature at the origin of the sound.</param>
        /// <param name="distance">The maximum distance from the source at which the sound can be heard.</param>
        public static void PlayWithinDistance(this ISound sound, ICreature source, int distance)
        {
            var message = sound.ToMessage();
            foreach (var character in source.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => c.Location.WithinDistance(source.Location, distance)))
            {
                character.Session.SendMessage(message);
            }
        }

        /// <summary>
        /// Plays a sound exclusively for a single character.
        /// </summary>
        /// <param name="sound">The sound to be played.</param>
        /// <param name="character">The character who will hear the sound.</param>
        public static void PlayFor(this ISound sound, ICharacter character) => character.Session.SendMessage(sound.ToMessage());
    }
}