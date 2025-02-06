using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    /// </summary>
    public class MusicTab : WidgetScript
    {
        /// <summary>
        ///     The on music play.
        /// </summary>
        private EventHappened _musicPlayHandler;

        public MusicTab(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.SetOptions(1, 0, MusicConstants.ConfigIDs.Length * 64, 30);
            InterfaceInstance.SetOptions(9, 0, 12, 30);
            InterfaceInstance.AttachClickHandler(1, (componentID, clickType, extraData1, extraData2) =>
            {
                var musicIndex = (int)Math.Floor(extraData2 * 0.5);
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Music.PlayMusic(musicIndex);
                    return true;
                }

                if (clickType == ComponentClickType.Option2Click)
                {
                    Owner.Music.SendHint(musicIndex);
                    return true;
                }

                if (clickType == ComponentClickType.Option3Click)
                {
                    Owner.Music.AddMusicToPlayList(musicIndex);
                    return true;
                }

                if (clickType == ComponentClickType.Option4Click)
                {
                    Owner.Music.RemoveMusicFromPlayList(musicIndex);
                    return true;
                }

                return false;
            });
            InterfaceInstance.AttachClickHandler(4, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Music.AddPlayingMusicToPlayList();
                    return true;
                }

                return false;
            });
            InterfaceInstance.AttachClickHandler(9, (componentID, clickType, extraData1, playListIndex) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Music.PlayMusicFromPlayList((byte)playListIndex);
                    return true;
                }

                if (clickType == ComponentClickType.Option2Click)
                {
                    Owner.Music.RemoveMusicFromPlayListByIndex(playListIndex);
                    return true;
                }

                return false;
            });
            InterfaceInstance.AttachClickHandler(11, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Music.TogglePlayList();
                    return true;
                }

                return false;
            });
            InterfaceInstance.AttachClickHandler(12, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Music.ClearPlayList();
                    return true;
                }

                return false;
            });
            InterfaceInstance.AttachClickHandler(14, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Music.ToggleShuffle();
                    return true;
                }

                return false;
            });

            _musicPlayHandler = Owner.RegisterEventHandler(new EventHappened<MusicPlayEvent>(e =>
            {
                RefreshPlayingMusic(e.MusicIndex);
                return false;
            }));

            RefreshPlayingMusic(Owner.Music.PlayingMusicIndex);
            Owner.Music.Refresh();
        }

        /// <summary>
        ///     Refreshes the playing music.
        /// </summary>
        /// <param name="musicIndex">Index of the music.</param>
        public void RefreshPlayingMusic(int musicIndex)
        {
            var musicName = Owner.Music.GetMusicName(musicIndex);
            InterfaceInstance.DrawString(4, musicName == string.Empty ? "No music playing..." : musicName);
        }

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
            if (_musicPlayHandler != null)
            {
                Owner.UnregisterEventHandler<MusicPlayEvent>(_musicPlayHandler);
            }
        }
    }
}