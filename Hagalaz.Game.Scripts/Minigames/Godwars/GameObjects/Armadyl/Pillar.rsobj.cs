using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Builders.Region;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Minigames.Godwars.GameObjects.Armadyl
{
    /// <summary>
    /// </summary>
    [GameObjectScriptMetaData([26303])]
    public class Pillar : GameObjectScript
    {
        private readonly IRegionUpdateBuilder _regionUpdateBuilder;
        private readonly IMovementBuilder _movementBuilder;

        public Pillar(IRegionUpdateBuilder regionUpdateBuilder, IMovementBuilder movementBuilder)
        {
            _regionUpdateBuilder = regionUpdateBuilder;
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
                if (clicker.HasState(StateType.CrossbowEquiped))
                {
                    if (clicker.HasState(StateType.MithGrappleEquiped))
                    {
                        clicker.Interrupt(this);
                        clicker.Movement.Lock(true);
                        ILocation destination;
                        var movementDirection = FaceDirection.None;

                        if (clicker.Location.Y > Owner.Location.Y)
                        {
                            destination = Location.Create(2872, 5269, 2, 0);
                            movementDirection = FaceDirection.South;
                        }
                        else
                        {
                            destination = Location.Create(2872, 5279, 2, 0);
                            movementDirection = FaceDirection.North;
                        }

                        clicker.QueueAnimation(Animation.Create(7081));
                        var update = _regionUpdateBuilder.Create()
                            .WithLocation(Location.Create(2872, 5274, 2, 0))
                            .WithGraphic(Graphic.Create(2103))
                            .Build();
                        Owner.Region.QueueUpdate(update);
                        clicker.FaceLocation(Owner.Location);

                        clicker.QueueTask(new RsTask(() =>
                            {
                                // north = direction 0
                                // south = direction 2

                                var mov = _movementBuilder
                                    .Create()
                                    .WithStart(clicker.Location)
                                    .WithEnd(destination)
                                    .WithEndSpeed(85)
                                    .WithFaceDirection(movementDirection)
                                    .Build();
                                clicker.QueueForceMovement(mov);
                                clicker.QueueTask(new RsTask(() =>
                                    {
                                        clicker.Movement.Teleport(Teleport.Create(destination));
                                        clicker.Movement.Unlock(false);
                                    },
                                    3));
                            },
                            2));
                    }
                    else
                    {
                        clicker.SendChatMessage("You need to have a Mith grapple equiped.");
                    }
                }
                else
                {
                    clicker.SendChatMessage("You need to have a crossbow equiped.");
                }

                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize() { }
    }
}