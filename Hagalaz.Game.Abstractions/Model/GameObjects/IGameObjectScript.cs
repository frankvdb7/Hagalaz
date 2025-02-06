using System;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    /// <summary>
    /// 
    /// </summary>
    public interface IGameObjectScript
    {
        /// <summary>
        /// Get's objectIDS which are suitable for this script.
        /// </summary>
        /// <returns></returns>
        [Obsolete("Please use GameObjectScriptMetaData attribute instead")]
        int[] GetSuitableObjects();

        /// <summary>
        /// Initializes this script with given owner.
        /// </summary>
        /// <param name="owner"></param>
        void Initialize(IGameObject owner);
        /// <summary>
        /// Happens when object is spawned.
        /// </summary>
        void OnSpawn();
        /// <summary>
        /// Happens when this object is enabled.
        /// </summary>
        void OnEnable();
        /// <summary>
        /// Happens when this object is disabled.
        /// </summary>
        void OnDisable();
        /// <summary>
        /// Happens when object is destroyed.
        /// </summary>
        void OnDestroy();
        /// <summary>
        /// Determines whether this instance [can render for] the specified creature.
        /// By default, returns true.
        /// </summary>
        /// <param name="creature">The creature.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can render for] the specified creature; otherwise, <c>false</c>.
        /// </returns>
        bool CanRenderFor(ICreature creature);
        /// <summary>
        /// Happens when this object is deleted for a specific character.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="character">The character.</param>
        void OnDeletedFor(ICharacter character);
        /// <summary>
        /// Happens when this object is rendered for a specific character.
        /// By default this method does nothing.
        /// </summary>
        /// <param name="character">The character.</param>
        void OnRenderedFor(ICharacter character);
        /// <summary>
        /// Determines whether this instance can be destroyed.
        /// By default, this method returns true.
        /// </summary>
        /// <returns></returns>
        bool CanDestroy();
        /// <summary>
        /// Get's if this object can be suspended.
        /// By default, this method returns true.
        /// </summary>
        /// <returns></returns>
        bool CanSuspend();
        /// <summary>
        /// Happens when character click's this object.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        /// <param name="forceRun">Wheter CTRL was clicked.</param>
        void OnCharacterClick(ICharacter clicker, GameObjectClickType clickType, bool forceRun);
        /// <summary>
        /// Called when [use item].
        /// By default, returns false: not handled.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        bool UseItemOnGameObject(IItem used, ICharacter character);
    }
}
