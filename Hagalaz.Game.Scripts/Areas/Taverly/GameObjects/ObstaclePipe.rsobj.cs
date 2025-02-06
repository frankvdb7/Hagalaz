using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.Taverly.GameObjects
{
    /// <summary>
    /// </summary>
    /// <seealso cref="GameObjectScript" />
    [GameObjectScriptMetaData([9293])]
    public class ObstaclePipe : GameObjectScript
    {
        private readonly IMovementBuilder _movementBuilder;

        public ObstaclePipe(IMovementBuilder movementBuilder)
        {
            _movementBuilder = movementBuilder;
        }

        /// <summary>
        ///     Happens when character click's this object and then walks to it
        ///     and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacterClick is overrided
        ///     than this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                if (clicker.Statistics.GetSkillLevel(StatisticsConstants.Agility) < 70)
                {
                    clicker.SendChatMessage("You need an agility level of 70 to use this obstacle.");
                    return;
                }

                clicker.Interrupt(this);
                clicker.Movement.MovementType = MovementType.Walk;
                var destination = Location.Create((short)(clicker.Location.X == 2886 ? 2892 : 2886), 9799, 0);
                clicker.Movement.Lock(false);

                clicker.QueueAnimation(Animation.Create(10580));

                var movement = _movementBuilder
                    .Create()
                    .WithStart(clicker.Location)
                    .WithEnd(destination)
                    .WithEndSpeed(150)
                    .Build();
                clicker.QueueForceMovement(movement);

                RsTickTask task = null!;
                task = new RsTickTask(() =>
                {
                    if (task.TickCount == 4)
                    {
                        clicker.QueueAnimation(Animation.Create(10579));
                        clicker.Movement.Teleport(Teleport.Create(destination));
                        clicker.Movement.Unlock(false);
                        task.Cancel();
                    }
                });
                clicker.QueueTask(task);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}