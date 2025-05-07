using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Agility.Courses.Gnome
{
    /// <summary>
    /// </summary>
    public class LogBalance : GameObjectScript
    {
        /// <summary>
        ///     The speak texts
        /// </summary>
        private static readonly string[] _speakTexts = ["Okay get over that log, quick quick!"];

        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                clicker.Interrupt(this);
                clicker.SendChatMessage("You walk carefully across the slippery log...");
                clicker.Movement.MovementType = MovementType.Walk;
                clicker.Movement.AddToQueue(Location.Create(2474, 3429, 0, clicker.Location.Dimension));
                clicker.Movement.Lock(false);
                RsTickTask task = null;
                task = new RsTickTask(() =>
                {
                    if (task.TickCount == 1)
                    {
                        clicker.Appearance.RenderId = 155;
                    }
                    else if (task.TickCount >= 8) // 8 ticks = 7 steps
                    {
                        clicker.Movement.Unlock(false);
                        clicker.Appearance.ResetRenderID();
                        clicker.Statistics.AddExperience(StatisticsConstants.Agility, 7.5);
                        clicker.SendChatMessage("...and make it safely to the other side.");
                        clicker.AddState(new State(StateType.GnomeCourseLogBalance, int.MaxValue));
                        clicker.ResetMovementType();
                        task.Cancel();
                    }
                });
                clicker.QueueTask(task);
                Agility.CheckGnomeCourseNpCs(clicker, _speakTexts[RandomStatic.Generator.Next(0, _speakTexts.Length)]);
            }
            else
            {
                base.OnCharacterClickPerform(clicker, clickType);
            }
        }

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
        }

        /// <summary>
        ///     Gets the suitable objects.
        /// </summary>
        /// <returns></returns>
        public override int[] GetSuitableObjects() => [69526];
    }
}