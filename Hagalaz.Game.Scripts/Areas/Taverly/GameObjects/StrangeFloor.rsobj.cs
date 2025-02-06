using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.Taverly.GameObjects
{
    /// <summary>
    ///     TODO: Object animation
    /// </summary>
    /// <seealso cref="GameObjectScript" />
    [GameObjectScriptMetaData([9294])]
    public class StrangeFloor : GameObjectScript
    {
        private readonly IMovementBuilder _movementBuilder;
        public StrangeFloor(IMovementBuilder movementBuilder)
        {
            _movementBuilder = movementBuilder;
        }

        /// <summary>
        ///     Called when [character click walk].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="canInteract">if set to <c>true</c> [can interact].</param>
        public override void OnCharacterClickReached(ICharacter clicker, GameObjectClickType clickType, bool canInteract)
        {
            if (!canInteract && clicker.WithinRange(Owner.Location, 1, 2))
            {
                OnCharacterClickPerform(clicker, clickType);
            }
            else
            {
                base.OnCharacterClickReached(clicker, clickType, canInteract);
            }
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
                if (clicker.Statistics.GetSkillLevel(StatisticsConstants.Agility) < 80)
                {
                    clicker.SendChatMessage("You need an agility level of 80 to use this obstacle.");
                    return;
                }

                var isSouth = clicker.Location.Y > 9813;
                var dest = isSouth ? Location.Create(2878, 9812, 0) : Location.Create(2880, 9814, 0);
                RsTickTask task = null;
                clicker.ForceRunMovementType(true);
                clicker.Movement.AddToQueue(Location.Create(isSouth ? 2881 : 2877, isSouth ? 9814 : 9812, 0));
                clicker.Movement.Lock(false);
                clicker.QueueTask(task = new RsTickTask(() =>
                {
                    if (task.TickCount == 1)
                    {
                        clicker.FaceLocation(Owner);
                        clicker.QueueAnimation(Animation.Create(1995));
                    }
                    else if (task.TickCount == 2)
                    {
                        var mov = _movementBuilder
                            .Create()
                            .WithStart(clicker.Location)
                            .WithEnd(dest)
                            .WithEndSpeed(70)
                            .Build();
                        clicker.QueueForceMovement(mov);
                        clicker.QueueAnimation(Animation.Create(1603));
                    }
                    else if (task.TickCount == 4)
                    {
                        clicker.Statistics.AddExperience(StatisticsConstants.Agility, 12.5);
                        clicker.Movement.Teleport(Teleport.Create(dest));
                        clicker.Movement.Unlock(false);
                        task.Cancel();
                    }
                }));
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