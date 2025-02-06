using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.NoticeBoard
{
    /// <summary>
    /// </summary>
    public class NoticeBoardQuest : WidgetScript
    {
        public NoticeBoardQuest(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
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
            InterfaceInstance.AttachClickHandler(7, (componentID, type, extraInfo1, extraInfo2) =>
            {
                // TODO - check
                //Owner.Session.SendPacketAsync(new GameFramePacketComposer(Owner.Interfaces.CurrentFrame)); // reset frame
                return false;
            });
    }
}