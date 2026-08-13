using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Extensions
{
    /// <summary>
    /// Provides extension methods for the <see cref="ICreature"/> interface, offering convenient shortcuts for common actions.
    /// </summary>
    public static class CreatureExtensions
    {
        /// <summary>
        /// Makes the creature face the location of a specified game object, taking its size into account.
        /// </summary>
        /// <param name="creature">The creature that will be turning.</param>
        /// <param name="gameObject">The game object to face.</param>
        public static void FaceLocation(this ICreature creature, IGameObject gameObject) => creature.FaceLocation(gameObject.Location, gameObject.SizeX, gameObject.SizeY);

        /// <summary>
        /// Makes the creature face the location of another creature, taking its size into account.
        /// </summary>
        /// <param name="creature">The creature that will be turning.</param>
        /// <param name="creatureToFace">The creature that will be faced.</param>
        public static void FaceLocation(this ICreature creature, ICreature creatureToFace) => creature.FaceLocation(creatureToFace.Location, creatureToFace.Size, creatureToFace.Size);

        /// <summary>
        /// Makes the creature face the location of a specified game object.
        /// </summary>
        /// <param name="creature">The creature that will be turning.</param>
        /// <param name="target">The game object to face.</param>
        public static void Face(this ICreature creature, IGameObject target)
        {
            creature.FaceLocation(target.Location);
        }

        /// <summary>
        /// Checks if the creature is dead.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <returns><c>true</c> if the creature is dead; otherwise, <c>false</c>.</returns>
        public static bool IsDead(this ICreature creature)
        {
            return creature.Combat.IsDead;
        }

        /// <summary>
        /// Checks if the creature is alive.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        /// <returns><c>true</c> if the creature is alive; otherwise, <c>false</c>.</returns>
        public static bool IsAlive(this ICreature creature)
        {
            return !creature.Combat.IsDead;
        }

        /// <summary>
        /// Queues an asynchronous task to be executed by the creature's task scheduler.
        /// </summary>
        /// <param name="creature">The creature that will execute the task.</param>
        /// <param name="task">The asynchronous action to be executed.</param>
        /// <returns>A handle to the queued task, which can be used to monitor or cancel it.</returns>
        public static IRsTaskHandle QueueTask(this ICreature creature, Func<Task> task) => creature.QueueTask(new RsAsyncTask(task));

        /// <summary>
        /// Starts asynchronous preparation from a synchronous game-loop task and queues its synchronous continuation when preparation completes.
        /// </summary>
        /// <param name="creature">The creature that owns the continuation.</param>
        /// <param name="prepare">The asynchronous preparation operation.</param>
        /// <returns>A handle to the synchronous task that starts preparation.</returns>
        public static IRsTaskHandle QueueAsyncTask(this ICreature creature, Func<Task<Action?>> prepare)
        {
            return creature.QueueTask(new RsTask(() => _ = CompleteAsync(creature, prepare), 1));
        }

        private static async Task CompleteAsync(ICreature creature, Func<Task<Action?>> prepare)
        {
            try
            {
                var continuation = await prepare().ConfigureAwait(false);
                if (continuation is not null)
                {
                    creature.QueueTask(new RsTask(continuation, 1));
                }
            }
            catch (Exception exception)
            {
                creature.QueueTask(new RsTask(() => ExceptionDispatchInfo.Capture(exception).Throw(), 1));
            }
        }

        /// <summary>
        /// Queues a synchronous task to be executed by the creature's task scheduler after a specified delay.
        /// </summary>
        /// <param name="creature">The creature that will execute the task.</param>
        /// <param name="task">The synchronous action to be executed.</param>
        /// <param name="executeDelay">The delay in game ticks before the task is executed.</param>
        /// <returns>A handle to the queued task, which can be used to monitor or cancel it.</returns>
        public static IRsTaskHandle QueueTask(this ICreature creature, Action task, int executeDelay) => creature.QueueTask(new RsTask(task, executeDelay));
    }
}
