using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Tasks;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena
{
    /// <summary>
    /// </summary>
    public class DuelStartTask : RsTickTask
    {
        /// <summary>
        ///     The performer
        /// </summary>
        private readonly ICharacter _performer;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DuelStartTask" /> class.
        /// </summary>
        /// <param name="performer">The performer.</param>
        public DuelStartTask(ICharacter performer)
        {
            _performer = performer;
            TickActionMethod = PerformTickImpl;
            _performer.Movement.Lock(true);
        }

        /// <summary>
        ///     Contains tick implementation.
        /// </summary>
        /// <returns></returns>
        private void PerformTickImpl()
        {
            if (TickCount == 3)
            {
                _performer.Speak("3");
            }
            else if (TickCount == 5)
            {
                _performer.Speak("2");
            }
            else if (TickCount == 7)
            {
                _performer.Speak("1");
            }
            else if (TickCount == 9)
            {
                _performer.Speak("FIGHT!");
                _performer.Movement.Unlock(true);
                Cancel();
            }
        }
    }
}