using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.NoticeBoard;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    /// </summary>
    public class NoticeBoardTab : WidgetScript
    {
        private readonly IWidgetBuilder _widgetBuilder;

        public NoticeBoardTab(ICharacterContextAccessor characterContextAccessor, IWidgetBuilder widgetBuilder) : base(characterContextAccessor)
        {
            _widgetBuilder = widgetBuilder;
        }

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen() =>
            InterfaceInstance.AttachClickHandler(57, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    var frame = _widgetBuilder
                        .Create()
                        .ForCharacter(Owner)
                        .WithId(753) // black background
                        .AsFrame()
                        .Build();
                    Owner.Widgets.OpenFrame(frame);
                    var widget = _widgetBuilder
                        .Create()
                        .ForCharacter(Owner)
                        .WithId(190)
                        .WithParentId(frame.Id)
                        .WithParentSlot(1)
                        .WithScript<NoticeBoardQuest>()
                        .Build();
                    Owner.Widgets.OpenWidget(widget);
                    return true;
                }

                return false;
            });
    }
}