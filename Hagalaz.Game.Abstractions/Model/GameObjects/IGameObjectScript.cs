using System;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Model.GameObjects
{
    /// <summary>
    /// Defines the contract for a script that controls a game object's behavior and logic.
    /// </summary>
    public interface IGameObjectScript
    {
        /// <summary>
        /// Gets the object IDs that this script is suitable for.
        /// </summary>
        /// <returns>An array of object IDs.</returns>
        [Obsolete("Please use GameObjectScriptMetaData attribute instead")]
        int[] GetSuitableObjects();

        /// <summary>
        /// Initializes the script with its owning game object.
        /// </summary>
        /// <param name="owner">The game object that this script is attached to.</param>
        void Initialize(IGameObject owner);

        /// <summary>
        /// A callback executed when the game object is spawned into the world.
        /// </summary>
        void OnSpawn();

        /// <summary>
        /// A callback executed when the game object is enabled (made interactive).
        /// </summary>
        void OnEnable();

        /// <summary>
        /// A callback executed when the game object is disabled (made non-interactive).
        /// </summary>
        void OnDisable();

        /// <summary>
        /// A callback executed when the game object is destroyed.
        /// </summary>
        void OnDestroy();

        /// <summary>
        /// Determines whether this game object should be rendered for a specific creature.
        /// </summary>
        /// <param name="creature">The creature viewing the object.</param>
        /// <returns><c>true</c> if the object should be rendered; otherwise, <c>false</c>.</returns>
        bool CanRenderFor(ICreature creature);

        /// <summary>
        /// A callback executed when this game object is deleted from a specific character's view.
        /// </summary>
        /// <param name="character">The character for whom the object was deleted.</param>
        void OnDeletedFor(ICharacter character);

        /// <summary>
        /// A callback executed when this game object is rendered for a specific character.
        /// </summary>
        /// <param name="character">The character for whom the object was rendered.</param>
        void OnRenderedFor(ICharacter character);

        /// <summary>
        /// Determines whether this game object can be permanently destroyed.
        /// </summary>
        /// <returns><c>true</c> if the object can be destroyed; otherwise, <c>false</c>.</returns>
        bool CanDestroy();

        /// <summary>
        /// Determines if this game object's processing can be suspended (e.g., when no players are nearby).
        /// </summary>
        /// <returns><c>true</c> if the object can be suspended; otherwise, <c>false</c>.</returns>
        bool CanSuspend();

        /// <summary>
        /// A callback executed when a character clicks on this game object.
        /// </summary>
        /// <param name="clicker">The character who clicked the object.</param>
        /// <param name="clickType">The type of click option selected.</param>
        /// <param name="forceRun">A value indicating whether the character should force-run to the object.</param>
        void OnCharacterClick(ICharacter clicker, GameObjectClickType clickType, bool forceRun);

        /// <summary>
        /// Handles the "Use item on object" action.
        /// </summary>
        /// <param name="used">The item being used on the object.</param>
        /// <param name="character">The character using the item.</param>
        /// <returns><c>true</c> if the action was handled by the script; otherwise, <c>false</c>.</returns>
        bool UseItemOnGameObject(IItem used, ICharacter character);
    }
}