using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Game.Scripts.Model.Widgets
{
    /// <summary>
    /// Interface for normal interface scripts.
    /// </summary>
    public abstract class WidgetScript : IWidgetScript
    {
        private readonly ICharacterContextAccessor _characterContextAccessor;

        /// <summary>
        /// Contains the owner of this interface script.
        /// </summary>
        protected ICharacter Owner => _characterContextAccessor.Context.Character;

        /// <summary>
        /// Contains the interface of this interface script.
        /// </summary>
        protected IWidget InterfaceInstance { get; private set; }

        public WidgetScript(ICharacterContextAccessor characterContextAccessor) => _characterContextAccessor = characterContextAccessor;

        /// <summary>
        /// Get's called when interface script instance is created.
        /// </summary>
        /// <param name="inter">Interface instance of opened interface.</param>
        public void Initialize(IWidget inter)
        {
            InterfaceInstance = inter;
            Initialize();
        }

        /// <summary>
        /// Get's called when interface script instance is created.
        /// </summary>
        protected virtual void Initialize() { }

        /// <summary>
        /// Happens when interface is opened for character.
        /// </summary>
        public abstract void OnOpen();

        /// <summary>
        /// Happens when interface has been opened for character.
        /// </summary>
        public virtual void OnOpened() { }

        /// <summary>
        /// Happens when interface is closed for character.
        /// </summary>
        public virtual void OnClose() { }

        /// <summary>
        /// Happens when interface has been closed for character.
        /// </summary>
        public virtual void OnClosed() { }

        /// <summary>
        /// Called when the display and the frame has been changed for the character.
        /// Calling this will re-register the interface on the new frame.
        /// </summary>
        public virtual void OnDisplayChanged() { }

        /// <summary>
        /// Determines whether this instance can be interrupted.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can be interrupted; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool CanInterrupt() => true;
    }
}