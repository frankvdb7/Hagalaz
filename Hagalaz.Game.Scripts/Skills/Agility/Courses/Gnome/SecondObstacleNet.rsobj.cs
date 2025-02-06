using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
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
    public class SecondObstacleNet : GameObjectScript
    {
        /// <summary>
        ///     The speak texts
        /// </summary>
        private static readonly string[] SpeakTexts = ["My Granny can move faster than you."];

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
                clicker.Movement.Lock(true);
                clicker.SendChatMessage("You climb the netting...");
                clicker.QueueAnimation(Animation.Create(828));
                ILocation destination = null;
                if (clicker.Location.Y == 3425)
                {
                    destination = Location.Create(clicker.Location.X, 3427, 0, clicker.Location.Dimension);
                }
                else if (clicker.Location.Y == 3427)
                {
                    destination = Location.Create(clicker.Location.X, 3425, 0, clicker.Location.Dimension);
                }

                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.Movement.Teleport(Teleport.Create(destination));
                        clicker.Statistics.AddExperience(StatisticsConstants.Agility, 7.5);
                        clicker.AddState(new State(StateType.GnomeCourseSecondObstacleNet, int.MaxValue));
                        clicker.Movement.Unlock(false);
                    }, 2));
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
        public override int[] GetSuitableObjects() => [69384];
    }
}