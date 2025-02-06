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
    public class BalancingRope : GameObjectScript
    {
        /// <summary>
        ///     The speak texts
        /// </summary>
        private static readonly string[] SpeakTexts = ["Come on scaredy cat, get across that rope!"];

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
                clicker.SendChatMessage("You carefully cross the tightrope.");
                clicker.Movement.MovementType = MovementType.Walk;
                ILocation destination = null;
                if (Owner.Id == 2312)
                {
                    destination = Location.Create(2483, 3420, 2, clicker.Location.Dimension);
                }
                else
                {
                    destination = Location.Create(2477, 3420, 2, clicker.Location.Dimension);
                }

                clicker.Movement.AddToQueue(destination);
                clicker.Movement.Lock(false);
                RsTickTask task = null;
                task = new RsTickTask(() =>
                {
                    if (task.TickCount == 1)
                    {
                        clicker.Appearance.RenderId = 155;
                    }
                    else if (task.TickCount >= 6) // 6 ticks = 5 tiles
                    {
                        clicker.Movement.Unlock(false);
                        clicker.Appearance.ResetRenderID();
                        clicker.Statistics.AddExperience(StatisticsConstants.Agility, 7.5);
                        clicker.AddState(new State(StateType.GnomeCourseBalancingRope, int.MaxValue));
                        clicker.ResetMovementType();
                        task.Cancel();
                    }
                });
                clicker.QueueTask(task);
                Agility.CheckGnomeCourseNpCs(clicker, SpeakTexts[RandomStatic.Generator.Next(0, SpeakTexts.Length)]);
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
        public override int[] GetSuitableObjects() => [2312, 4059];
    }
}