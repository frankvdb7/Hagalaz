using System;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Resources;

namespace Hagalaz.Game.Scripts.Model.GameObjects
{
    /// <summary>
    /// Base ObjectScript class.
    /// </summary>
    public abstract class GameObjectScript : IGameObjectScript
    {
        /// <summary>
        /// Contains owner object.
        /// </summary>
        protected IGameObject Owner { get; private set; } = default!;

        /// <summary>
        /// Get's objectIDS which are suitable for this script.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use the GameObjectScriptMetadata attribute instead")]
        public virtual int[] GetSuitableObjects() => [];

        /// <summary>
        /// Get's called when owner is found.
        /// </summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// Initializes this script with given owner.
        /// </summary>
        /// <param name="owner"></param>
        public void Initialize(IGameObject owner)
        {
            Owner = owner;
            Initialize();
        }

        /// <summary>
        /// Happens when object is spawned.
        /// </summary>
        public virtual void OnSpawn() { }

        /// <summary>
        /// Determines whether this instance can be destroyed.
        /// By default, this method returns true.
        /// </summary>
        /// <returns></returns>
        public virtual bool CanDestroy() => true;

        /// <summary>
        /// Happens when object is destroyed.
        /// </summary>
        public virtual void OnDestroy() { }

        /// <summary>
        /// Happens when this object is disabled.
        /// </summary>
        public virtual void OnDisable() { }

        /// <summary>
        /// Happens when this object is enabled.
        /// </summary>
        public virtual void OnEnable() { }

        /// <summary>
        /// Get's if this object can be suspended.
        /// By default, this method returns true.
        /// </summary>
        /// <returns></returns>
        public virtual bool CanSuspend() => true;

        /// <summary>
        /// Happens when this object is rendered for a specific character.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="character">The character.</param>
        public virtual void OnRenderedFor(ICharacter character) { }

        /// <summary>
        /// Happens when this object is deleted for a specific character.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="character">The character.</param>
        public virtual void OnDeletedFor(ICharacter character) { }

        /// <summary>
        /// Determines whether this instance [can render for] the specified creature.
        /// By default, returns true.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can render for] the specified creature; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanRenderFor(ICreature creature) => true;

        /// <summary>
        /// Called when [use item].
        /// By default, returns false: not handled.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public virtual bool UseItemOnGameObject(IItem used, ICharacter character) => false;

        /// <summary>
        /// Called when [character click walk].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="canInteract">if set to <c>true</c> [can interact].</param>
        public virtual void OnCharacterClickReached(ICharacter clicker, GameObjectClickType clickType, bool canInteract)
        {
            if (canInteract)
                OnCharacterClickPerform(clicker, clickType);
            else
            {
                if (clicker.HasState(StateType.Frozen))
                    clicker.SendChatMessage(GameStrings.MagicalForceMovement);
                else
                    clicker.SendChatMessage(GameStrings.YouCantReachThat);
            }
        }

        /// <summary>
        /// Happens when character click's this object.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        /// <param name="forceRun">Wheter CTRL was clicked.</param>
        public virtual void OnCharacterClick(ICharacter clicker, GameObjectClickType clickType, bool forceRun)
        {
            clicker.Interrupt(this);
            if (clickType == GameObjectClickType.Option6Click)
            {
                if (clicker.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                    clicker.SendChatMessage(Owner.ToString()!, ChatMessageType.ConsoleText);
                clicker.SendChatMessage(Owner.Definition.Examine);
            }
            else
            {
                if (clicker.EventManager.SendEvent(new WalkAllowEvent(clicker, Owner.Location, forceRun, false)))
                {
                    clicker.Movement.MovementType = clicker.Movement.MovementType == MovementType.Run || forceRun ? MovementType.Run : MovementType.Walk;
                    clicker.QueueTask(new GameObjectReachTask(clicker, Owner, (success) => OnCharacterClickReached(clicker, clickType, success)));
                }
            }
        }

        /// <summary>
        /// Happens when character click's this object and then walks to it
        /// and reaches it.
        /// This method is called by OnCharacterClick by default, if OnCharacterClick is overrided
        /// than this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        /// <returns></returns>
        public virtual void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            clicker.EventManager.SendEvent(new GameObjectClickPerformEvent(clicker, Owner, clickType));
            if (clicker.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                clicker.SendChatMessage("obj[id=" + Owner.Id + ",loc=(" + Owner.Location + "),type=" + clickType + "]", ChatMessageType.ConsoleText);
            else
                clicker.SendChatMessage(GameStrings.NothingInterestingHappens);
        }
    }
}