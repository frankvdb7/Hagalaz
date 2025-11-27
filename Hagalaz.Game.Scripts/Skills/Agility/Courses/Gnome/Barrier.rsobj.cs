using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Agility.Courses.Gnome
{
    [GameObjectScriptMetaData([69389])]
    public class Barrier : GameObjectScript
    {
        private readonly IMovementBuilder _movementBuilder;

        public Barrier(IMovementBuilder movementBuilder) => _movementBuilder = movementBuilder;

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
                clicker.QueueAnimation(Animation.Create(2923));
                // jump over barrier
                var movement = _movementBuilder
                    .Create()
                    .WithStart(clicker.Location)
                    .WithEnd(Location.Create(2485, 3434, 0, clicker.Location.Dimension))
                    .WithEndSpeed(90)
                    .Build();
                clicker.QueueForceMovement(movement);

                var direction = movement.FaceDirection;
                RsTickTask task = null;
                task = new RsTickTask(() =>
                {
                    if (task.TickCount == 2)
                    {
                        clicker.Movement.Teleport(Teleport.Create(movement.EndLocation.Clone()));
                        clicker.Appearance.Visible = false;
                    }
                    else if (task.TickCount == 3)
                    {
                        movement = _movementBuilder
                            .Create()
                            .WithStart(clicker.Location)
                            .WithEnd(Location.Create(2485, 3436, 1, clicker.Location.Dimension))
                            .WithEndSpeed(60)
                            .WithFaceDirection(direction)
                            .Build();
                        clicker.QueueForceMovement(movement);
                    }
                    else if (task.TickCount == 4)
                    {
                        clicker.Movement.Teleport(Teleport.Create(movement.EndLocation.Clone()));
                    }
                    else if (task.TickCount == 5)
                    {
                        movement = _movementBuilder
                            .Create()
                            .WithStart(clicker.Location)
                            .WithEnd(Location.Create(2485, 3436, 0, clicker.Location.Dimension))
                            .WithEndSpeed(60)
                            .WithFaceDirection(direction)
                            .Build();
                        clicker.QueueForceMovement(movement);
                    }
                    else if (task.TickCount == 6)
                    {
                        clicker.Appearance.Visible = true;
                        clicker.Movement.Teleport(Teleport.Create(movement.EndLocation.Clone()));
                        clicker.QueueAnimation(Animation.Create(2924));
                    }
                    else if (task.TickCount == 7)
                    {
                        clicker.Movement.Unlock(false);
                        clicker.AddState(new GnomeCourseBarrierState());
                        clicker.Statistics.AddExperience(StatisticsConstants.Agility, 25);
                        Agility.CheckGnomeCourseCompletion(clicker);
                    } 
                    else if (task.TickCount > 7)
                    {
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
