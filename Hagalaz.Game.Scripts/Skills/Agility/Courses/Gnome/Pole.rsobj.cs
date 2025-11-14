using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Skills.Agility.Courses.Gnome
{
    [GameObjectScriptMetaData([43529])]
    public class Pole : GameObjectScript
    {
        private readonly IMovementBuilder _movementBuilder;

        public Pole(IMovementBuilder movementBuilder) => _movementBuilder = movementBuilder;

        /// <summary>
        ///     Called when [character click].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="forceRun">if set to <c>true</c> [force run].</param>
        public override void OnCharacterClick(ICharacter clicker, GameObjectClickType clickType, bool forceRun)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                if (!clicker.EventManager.SendEvent(new WalkAllowEvent(clicker, Owner.Location, forceRun, false)))
                {
                    return;
                }

                if (Owner.Location.WithinDistance(clicker.Location, 6))
                {
                    LocationReachTask task = null;
                    task = new LocationReachTask(clicker, Location.Create(clicker.Location.X, 3418, 3, clicker.Location.Dimension), success =>
                    {
                        if (!success)
                        {
                            return;
                        }

                        clicker.FaceLocation(Owner);
                        OnCharacterClickPerform(clicker, clickType);
                    });
                    clicker.QueueTask(task);
                }
                else
                {
                    clicker.SendChatMessage(GameStrings.YouCantReachThat);
                }

                return;
            }

            base.OnCharacterClick(clicker, clickType, forceRun);
        }

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
                // pre-run
                clicker.QueueAnimation(Animation.Create(11784));
                var movement = _movementBuilder
                    .Create()
                    .WithStart(Location.Create(clicker.Location.X, 3418, 3, clicker.Location.Dimension))
                    .WithEnd(Location.Create(clicker.Location.X, 3421, 3, clicker.Location.Dimension))
                    .WithEndSpeed(60)
                    .Build();
                clicker.QueueForceMovement(movement);

                RsTickTask task = null;
                task = new RsTickTask(() =>
                {
                    if (task.TickCount == 2)
                    {
                        clicker.Movement.Teleport(Teleport.Create(movement.EndLocation));
                    }
                    else if (task.TickCount == 3)
                    {
                        // first jump
                        clicker.QueueAnimation(Animation.Create(11785));
                        movement = _movementBuilder
                            .Create()
                            .WithStart(clicker.Location)
                            .WithEnd(Location.Create(clicker.Location.X, 3425, 3, clicker.Location.Dimension))
                            .WithEndSpeed(30)
                            .Build();
                        clicker.QueueForceMovement(movement);
                    }
                    else if (task.TickCount == 4)
                    {
                        // first pole
                        clicker.QueueAnimation(Animation.Create(11789));
                        clicker.Movement.Teleport(Teleport.Create(movement.EndLocation));
                    }
                    else if (task.TickCount == 6)
                    {
                        // swing to second pole
                        movement = _movementBuilder
                            .Create()
                            .WithStart(clicker.Location)
                            .WithEnd(Location.Create(clicker.Location.X, 3429, 3, clicker.Location.Dimension))
                            .WithStartSpeed(60)
                            .WithEndSpeed(30)
                            .Build();
                        clicker.QueueForceMovement(movement);
                    }
                    else if (task.TickCount == 9)
                        // second pole
                    {
                        clicker.Movement.Teleport(Teleport.Create(movement.EndLocation));
                    }
                    else if (task.TickCount == 11)
                    {
                        // swing to platform
                        movement = _movementBuilder
                            .Create()
                            .WithStart(clicker.Location)
                            .WithEnd(Location.Create(clicker.Location.X, 3432, 3, clicker.Location.Dimension))
                            .WithStartSpeed(60)
                            .WithEndSpeed(30)
                            .Build();
                        clicker.QueueForceMovement(movement);
                    }
                    else if (task.TickCount == 14)
                    {
                        // platform
                        clicker.Movement.Teleport(Teleport.Create(movement.EndLocation));
                        clicker.AddState(new GnomeCoursePoleState());
                        clicker.Statistics.AddExperience(StatisticsConstants.Agility, 25);
                        clicker.Movement.Unlock(false);
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
