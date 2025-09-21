using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Extensions
{
    public static class CreatureExtensions
    {
        /// <summary>
        ///     Turn's (Faces) to specific game object.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="gameObject">The game obj.</param>
        public static void FaceLocation(this ICreature creature, IGameObject gameObject) => creature.FaceLocation(gameObject.Location, gameObject.SizeX, gameObject.SizeY);

        /// <summary>
        ///     Turn's (Faces) to specific creature location.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="creatureToFace">The creature to face.</param>
        public static void FaceLocation(this ICreature creature, ICreature creatureToFace) => creature.FaceLocation(creatureToFace.Location, creatureToFace.Size, creatureToFace.Size);

        public static void Face(this ICreature creature, IGameObject target)
        {
            creature.FaceLocation(target.Location);
        }

        public static bool IsDead(this ICreature creature)
        {
            return creature.Combat.IsDead;
        }

        public static bool IsAlive(this ICreature creature)
        {
            return !creature.Combat.IsDead;
        }

        /// <summary>
        /// Queue's task to be performed.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="task">The task.</param>
        public static IRsTaskHandle QueueTask(this ICreature creature, Func<Task> task) => creature.QueueTask(new RsAsyncTask(task));
        /// <summary>
        /// Queue's task to be performed.
        /// </summary>
        /// <param name="creature"></param>
        /// <param name="task"></param>
        /// <param name="executeDelay"></param>
        public static IRsTaskHandle QueueTask(this ICreature creature, Action task, int executeDelay) => creature.QueueTask(new RsTask(task, executeDelay));
    }
}
