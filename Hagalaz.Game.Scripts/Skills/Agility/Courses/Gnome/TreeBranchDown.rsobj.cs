using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Model;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Agility.Courses.Gnome
{
    /// <summary>
    /// </summary>
    public class TreeBranchDown : GameObjectScript
    {
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
                clicker.SendChatMessage("You climb down the tree...");
                clicker.QueueAnimation(Animation.Create(828));
                clicker.QueueTask(new RsTask(() =>
                    {
                        clicker.SendChatMessage("...you land on the ground.");
                        clicker.Movement.Teleport(Teleport.Create(Location.Create(2487, 3421, 0, clicker.Location.Dimension)));
                        clicker.Statistics.AddExperience(StatisticsConstants.Agility, 5);
                        clicker.AddState(new State(StateType.GnomeCourseTreeBranchDown, int.MaxValue));
                        clicker.Movement.Unlock(false);
                    }, 2));
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
        public override int[] GetSuitableObjects() => [69507];
    }
}