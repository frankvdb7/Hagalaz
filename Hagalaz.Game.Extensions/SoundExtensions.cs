using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Sound;

namespace Hagalaz.Game.Extensions
{
    public static class SoundExtensions
    {
        /// <summary>
        /// Plays a sound within the distance of the source.
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="source"></param>
        /// <param name="distance"></param>
        public static void PlayWithinDistance(this ISound sound, ICreature source, int distance)
        {
            var message = sound.ToMessage();
            foreach (var character in source.Viewport.VisibleCreatures.OfType<ICharacter>().Where(c => c.Location.WithinDistance(source.Location, distance)))
            {
                character.Session.SendMessage(message);
            }
        }

        public static void PlayFor(this ISound sound, ICharacter character) => character.Session.SendMessage(sound.ToMessage());
    }
}