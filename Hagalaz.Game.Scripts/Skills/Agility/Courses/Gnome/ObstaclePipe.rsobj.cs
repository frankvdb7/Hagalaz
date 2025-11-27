using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Agility.Courses.Gnome
{
    /// <summary>
    ///     TODO: Add enter / exit animations - check render id
    /// </summary>
    [GameObjectScriptMetaData([69377, 69378])]
    public class ObstaclePipe : GameObjectScript
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
                clicker.Movement.MovementType = MovementType.Walk;
                ILocation destination;
                if (clicker.Location.Y <= 3431)
                {
                    clicker.Movement.Teleport(Teleport.Create(Location.Create(Owner.Location.X, 3430, 0, clicker.Location.Dimension)));
                    destination = Location.Create(Owner.Location.X, 3437, 0, clicker.Location.Dimension);
                }
                else
                {
                    clicker.Movement.Teleport(Teleport.Create(Location.Create(Owner.Location.X, 3437, 0, clicker.Location.Dimension)));
                    destination = Location.Create(Owner.Location.X, 3430, 0, clicker.Location.Dimension);
                }

                clicker.Movement.AddToQueue(destination);
                clicker.Movement.Lock(false);
                RsTickTask task = null;
                task = new RsTickTask(() =>
                {
                    /*if (task.TickCount == 1)
                        clicker.QueueAnimation(Animation.Create(10580));
                    else*/
                    if (task.TickCount == 2)
                    {
                        clicker.Appearance.RenderId = 295;
                    }
                    else if (task.TickCount == 7)
                    {
                        clicker.Appearance.ResetRenderID();
                        //clicker.QueueAnimation(Animation.Create(10579));
                    }
                    else if (task.TickCount >= 8) // 8 ticks = 7 steps
                    {
                        clicker.Movement.Unlock(false);
                        clicker.Statistics.AddExperience(StatisticsConstants.Agility, 7.5);
                        clicker.ResetMovementType();
                        clicker.AddState(new GnomeCourseObstaclePipeState());
                        Agility.CheckGnomeCourseCompletion(clicker);
                        task.Cancel();
                    }
                });
                clicker.QueueTask(task);
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
    }
}
