using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character.Packet;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.CapeDesign
{
    /// <summary>
    ///     Contains color interface script.
    /// </summary>
    public class ColorInterfaceScript : WidgetScript
    {
        public ColorInterfaceScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     Contains color selected event unEquipHandler.
        /// </summary>
        private EventHappened? _colorSelectedHandler;

        /// <summary>
        ///     The item that this interface script will customize.
        /// </summary>
        public IItem ToCustomize { get; set; }

        /// <summary>
        ///     The part that this script will colorize.
        /// </summary>
        public int Part { get; set; }

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
            if (_colorSelectedHandler != null)
            {
                Owner.UnregisterEventHandler<ColorSelectedEvent>(_colorSelectedHandler);
            }

            var customizeScript = Owner.ServiceProvider.GetRequiredService<CustomizeInterfaceScript>();
            customizeScript.ToCustomize = ToCustomize;
            Owner.Widgets.OpenWidget(20, 0, customizeScript, false);
        }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
            _colorSelectedHandler = Owner.RegisterEventHandler(new EventHappened<ColorSelectedEvent>(e =>
            {
                if (e.SelectedColorID != -1)
                {
                    Owner.Appearance.GetOrAddItemPart(ToCustomize.Id).SetModelPartColor(Part, e.SelectedColorID); // cape
                    Owner.Appearance.GetOrAddItemPart(ToCustomize.Id + 1).SetModelPartColor(Part, e.SelectedColorID); // hat
                }

                Owner.Widgets.CloseWidget(InterfaceInstance);
                return false;
            }));

            InterfaceInstance.AttachClickHandler(21, (componentID, type, extraInfo1, extraInfo2) =>
            {
                if (type == ComponentClickType.LeftClick)
                {
                    Owner.Widgets.CloseWidget(InterfaceInstance);
                    return true;
                }

                return false;
            });
        }
    }
}