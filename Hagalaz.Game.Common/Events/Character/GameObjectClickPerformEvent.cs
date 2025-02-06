using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// Event which happens when an gameobject is clicked and the click perform action is called.
    /// </summary>
    public class GameObjectClickPerformEvent : CharacterEvent
    {
        /// <summary>
        /// Game object which was clicked.
        /// </summary>
        /// <value>The game object.</value>
        public IGameObject GameObject { get; }

        /// <summary>
        /// Gets the type of the click.
        /// </summary>
        /// <value>
        /// The type of the click.
        /// </value>
        public GameObjectClickType ClickType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameObjectClickPerformEvent"/> class.
        /// </summary>
        /// <param name="c">The character that will receive the event.</param>
        /// <param name="gameObject">The game object.</param>
        /// <param name="clickType">Type of the click.</param>
        public GameObjectClickPerformEvent(ICharacter c, IGameObject gameObject, GameObjectClickType clickType)
            : base(c)
        {
            GameObject = gameObject;
            ClickType = clickType;
        }
    }
}