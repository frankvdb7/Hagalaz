using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects.Altars
{
    /// <summary>
    ///     An altar that opens a <typeparamref name="TDialogue"/> to switch spell book or prayer book when clicked.
    /// </summary>
    /// <typeparam name="TDialogue">The dialogue the altar opens.</typeparam>
    public abstract class SwitchBookAltar<TDialogue> : GameObjectScript where TDialogue : SwitchBookDialogue
    {
        /// <summary>
        ///     The click that opens the dialogue. Other clicks behave as for any object.
        /// </summary>
        protected virtual GameObjectClickType SwitchClick => GameObjectClickType.Option1Click;

        /// <inheritdoc />
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == SwitchClick)
            {
                OpenDialogue(clicker);
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        private void OpenDialogue(ICharacter clicker) =>
            clicker.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.Send2Options, 0, clicker.ServiceProvider.GetRequiredService<TDialogue>(), true, Owner);
    }
}
