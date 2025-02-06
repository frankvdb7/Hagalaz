using System;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character.Packet;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Clans
{
    /// <summary>
    ///     Contains color interface script.
    /// </summary>
    public class MottifColourScript : WidgetScript
    {
        public MottifColourScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) {}

        /// <summary>
        ///     Contains color selected event unEquipHandler.
        /// </summary>
        private EventHappened? _colorSelectedHandler;

        /// <summary>
        ///     The on close
        /// </summary>
        public Action<int> OnSelectedColour { get; set; }

        /// <summary>
        ///     The selected colour.
        /// </summary>
        private int _selectedColour;

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
            if (_colorSelectedHandler != null)
            {
                Owner.UnregisterEventHandler<ColorSelectedEvent>(_colorSelectedHandler);
                _colorSelectedHandler = null;
            }

            OnSelectedColour?.Invoke(_selectedColour);
        }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.SetOptions(38, 0, 100, 2);
            _colorSelectedHandler = Owner.RegisterEventHandler(new EventHappened<ColorSelectedEvent>(e =>
            {
                _selectedColour = e.SelectedColorID;
                Owner.Widgets.CloseWidget(InterfaceInstance);
                return false;
            }));
        }
    }
}