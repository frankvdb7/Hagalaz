using System;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Warning
{
    /// <summary>
    /// </summary>
    public class WarningInterfaceScript : WidgetScript
    {
        public WarningInterfaceScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     The on clicked.
        /// </summary>
        public Action OnAcceptClicked { get; set; }

        /// <summary>
        ///     The on decline clicked.
        /// </summary>
        public Action? OnDeclineClicked { get; set; }

        /// <summary>
        ///     Gets the accept component identifier.
        /// </summary>
        /// <value>
        ///     The accept component identifier.
        /// </value>
        public int AcceptComponentID { get; set; } = 15;

        /// <summary>
        ///     Gets the decline component identifier.
        /// </summary>
        /// <value>
        ///     The decline component identifier.
        /// </value>
        public int DeclineComponentID { get; set; } = 16;

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.AttachClickHandler(AcceptComponentID, (componentID, clickType, extraData1, extraData2) =>
            {
                OnAcceptClicked?.Invoke();
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return true;
            });

            InterfaceInstance.AttachClickHandler(DeclineComponentID, (componentID, clickType, extraData1, extraData2) =>
            {
                OnDeclineClicked?.Invoke();
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return true;
            });
        }
    }
}