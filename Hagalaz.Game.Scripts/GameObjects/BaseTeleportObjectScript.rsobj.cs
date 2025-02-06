using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects
{
    /// <summary>
    /// </summary>
    public abstract class BaseTeleportObjectScript : GameObjectScript
    {
        /// <summary>
        ///     Gets the destination.
        /// </summary>
        /// <value>
        ///     The destination.
        /// </value>
        protected abstract ILocation Destination { get; }

        /// <summary>
        ///     Gets the delay.
        /// </summary>
        /// <value>
        ///     The delay.
        /// </value>
        protected virtual int Delay { get; } = 2;

        /// <summary>
        ///     Gets the animation.
        /// </summary>
        /// <value>
        ///     The animation.
        /// </value>
        protected virtual IAnimation TeleAnimation => Animation.Create(827);

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
                PerformTeleport(clicker);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Performs the teleport.
        /// </summary>
        /// <param name="character">The character.</param>
        protected virtual void PerformTeleport(ICharacter character)
        {
            character.Interrupt(this);
            character.Movement.Lock(true);
            character.QueueAnimation(TeleAnimation);
            character.QueueTask(new RsTask(() =>
                {
                    character.Movement.Teleport(Teleport.Create(Destination));
                    character.Movement.Unlock(false);
                }, Delay));
        }
    }
}