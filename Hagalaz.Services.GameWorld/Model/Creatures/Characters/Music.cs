using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Abstractions.Types.Providers;
using Hagalaz.Collections.Extensions;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Hagalaz.Game.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public class Music : IMusic, IHydratable<HydratedMusicDto>, IDehydratable<HydratedMusicDto>
    {
        /// <summary>
        /// The play list configuration ids
        /// </summary>
        private static readonly int[] _playListConfigIDs =
        [
            1621, 1622, 1623, 1624, 1625, 1626
        ];

        /// <summary>
        /// The owner.
        /// </summary>
        private readonly ICharacter _owner;

        private readonly IClientMapDefinitionProvider _clientMapDefinitionProvider;

        /// <summary>
        /// The unlocked music.
        /// </summary>
        private readonly HashSet<int> _unlockedMusic =
        [
            5, // Newbie Melody
            89, // Newbie Melody
            103, // Scape Original
            150, // Scape Main
            151, // Ground Scape
            152, // Scape Scared
            153, // Scape Santa
            196, // Time out
            200, // Artistry
            316, // Evil Bob's Island
            318, // The Quizmaster
            321, // Pheasant Peasant
            323, // Corporal Punishment
            336, // Frogland
            350, // Homescape
            360, // In the Clink
            377, // Scape Hunter
            411, // Head to Head
            412, // Pinball Wizard
            482, // School's out
            514, // Tournament!
            517, // Bounty Hunter Level 1
            518, // Bounty Hunter Level 2
            519, // Bounty Hunter Level 3
            520, // The Adventurer
            602, // Scape Summon
            611, // The Mentor
            650, // Snack Attack
            717, // Scape Theme
            931
        ];

        /// <summary>
        /// The play list.
        /// </summary>
        private List<int> _playList = new(12);

        /// <summary>
        /// The current play list index.
        /// </summary>
        private int _currentPlayListIndex = 0;

        /// <summary>
        /// contains the index of the playing music.
        /// </summary>
        public int PlayingMusicIndex { get; private set; }

        /// <summary>
        /// contains a value indicating whether [play list toggled].
        /// </summary>
        public bool IsPlayListToggled { get; private set; }

        /// <summary>
        /// Contains a value indicating whether this <see cref="Music"/> is shuffle.
        /// </summary>
        public bool IsShuffleToggled { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Music"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public Music(ICharacter owner)
        {
            _owner = owner;
            PlayingMusicIndex = -1;
            _clientMapDefinitionProvider = owner.ServiceProvider.GetRequiredService<IClientMapDefinitionProvider>();
        }

        /// <summary>
        /// Plays music by id.
        /// </summary>
        /// <param name="musicIndex">Index of the music.</param>
        public void PlayMusic(int musicIndex)
        {
            PlayingMusicIndex = musicIndex;
            var musicID = -1;
            if (_unlockedMusic.Contains(musicIndex))
            {
                var def = _clientMapDefinitionProvider.Get(1351);
                if (def != null)
                {
                    musicID = def.GetIntValue(musicIndex);
                }

                if (_owner.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                    _owner.SendChatMessage("music[index=" + musicIndex + ",id=" + musicID + "]", ChatMessageType.ConsoleText);
            }
            _owner.Session.SendMessage(new PlayMusicMessage
            {
                Id = musicID,
                Volume = 255,
                Delay = 100
            });
            new MusicPlayEvent(_owner, musicIndex).Send();
        }

        /// <summary>
        /// Plays the music from play list.
        /// </summary>
        /// <param name="playListIndex">Index of the play list.</param>
        public void PlayMusicFromPlayList(int playListIndex)
        {
            if (playListIndex >= _playList.Count)
                return;
            PlayMusic(_playList[playListIndex]);
        }

        /// <summary>
        /// Plays the next music from play list.
        /// </summary>
        public void PlayNextMusicFromPlayList()
        {
            if (IsShuffleToggled)
            {
                _currentPlayListIndex = RandomStatic.Generator.Next(0, _playList.Count);
                if (_playList.Count > 1 && PlayingMusicIndex == _playList[_currentPlayListIndex])
                {
                    if (_currentPlayListIndex < _playList.Count)
                        _currentPlayListIndex++;
                    else
                        _currentPlayListIndex = 0;
                }
            }
            else
            {
                if (PlayingMusicIndex != -1 && _currentPlayListIndex < _playList.Count)
                    _currentPlayListIndex++;
                else
                    _currentPlayListIndex = 0;
            }

            PlayMusicFromPlayList(_currentPlayListIndex);
        }

        /// <summary>
        /// Sends the music hint by the music index.
        /// </summary>
        /// <param name="musicIndex">The index.</param>
        public void SendHint(int musicIndex)
        {
            _owner.QueueTask(async () =>
            {
                var service = _owner.ServiceProvider.GetRequiredService<IMusicService>();
                var music = await service.FindMusicByIndex(musicIndex);
                if (music != null)
                {
                    _owner.SendChatMessage(music.Hint);
                }
            });
        }

        /// <summary>
        /// Unlocks the music.
        /// </summary>
        /// <param name="musicIndex">Index of the music.</param>
        /// <returns></returns>
        public bool UnlockMusic(int musicIndex)
        {
            if (!_unlockedMusic.Contains(musicIndex))
            {
                _unlockedMusic.Add(musicIndex);
                _owner.SendChatMessage("<col=ff0000>You have unlocked a new music track: " + GetMusicName(musicIndex) + ".");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the name of the music.
        /// </summary>
        /// <param name="musicIndex">Index of the music.</param>
        /// <returns></returns>
        public string GetMusicName(int musicIndex)
        {
            if (musicIndex == -1)
                return string.Empty;
            var definition = _clientMapDefinitionProvider.Get(1345);
            return definition?.GetStringValue(musicIndex) ?? string.Empty;
        }

        /// <summary>
        /// Adds the music to play list.
        /// </summary>
        /// <param name="musicIndex">Index of the music.</param>
        public void AddMusicToPlayList(int musicIndex)
        {
            if (_playList.Count < _playList.Capacity && !_playList.Contains(musicIndex))
            {
                _playList.Add(musicIndex);
                _owner.Configurations.SendBitConfiguration(7081 + _playList.Count - 1, musicIndex);
                if (IsPlayListToggled)
                    PlayNextMusicFromPlayList();
            }
        }

        /// <summary>
        /// Adds the playing music to play list.
        /// </summary>
        public void AddPlayingMusicToPlayList()
        {
            if (PlayingMusicIndex != -1)
                AddMusicToPlayList(PlayingMusicIndex);
            else
                _owner.SendChatMessage("There is no music playing.");
        }

        /// <summary>
        /// Removes the music from play list.
        /// </summary>
        /// <param name="musicIndex">Index of the music.</param>
        public void RemoveMusicFromPlayList(int musicIndex)
        {
            if (_playList.Contains(musicIndex))
            {
                _playList.Remove(musicIndex);
                _owner.Configurations.SendBitConfiguration(7081 + _playList.Count, short.MaxValue);
            }
        }

        /// <summary>
        /// Removes the index of the music from play list by.
        /// </summary>
        /// <param name="playListIndex">Index of the play list.</param>
        public void RemoveMusicFromPlayListByIndex(int playListIndex)
        {
            if (playListIndex < 0 || playListIndex >= _playList.Count)
                return;
            RemoveMusicFromPlayList(_playList[playListIndex]);
        }

        /// <summary>
        /// Clears the play list.
        /// </summary>
        public void ClearPlayList()
        {
            if (_playList.Count <= 0)
                return;
            _playList.Clear();
            RefreshPlayList();
        }

        /// <summary>
        /// Toggles the play list.
        /// </summary>
        public void TogglePlayList()
        {
            IsPlayListToggled = !IsPlayListToggled;
            if (!IsPlayListToggled)
                PlayMusic(-1);
            else
                PlayNextMusicFromPlayList();
            RefreshPlayListToggle();
        }

        /// <summary>
        /// Toggles the shuffle.
        /// </summary>
        public void ToggleShuffle()
        {
            IsShuffleToggled = !IsShuffleToggled;
            RefreshShuffle();
        }

        /// <summary>
        /// Called when [music played].
        /// </summary>
        /// <param name="musicID">The music identifier.</param>
        /// <returns></returns>
        public void OnMusicPlayed(int musicID)
        {
            if (IsPlayListToggled)
                PlayNextMusicFromPlayList();
        }

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        public void Refresh()
        {
            RefreshMusicList();
            RefreshPlayList();
            RefreshPlayListToggle();
            RefreshShuffle();
        }

        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        public void RefreshMusicList()
        {
            try
            {
                var configValues = new int[MusicConstants.ConfigIDs.Length];
                foreach (var index in _unlockedMusic)
                {
                    var configIndex = (index + 1) / 32;
                    if (configIndex >= MusicConstants.ConfigIDs.Length)
                        configIndex = MusicConstants.ConfigIDs.Length - 1;
                    configValues[configIndex] |= 1 << index - index * 32;
                }

                for (var i = 0; i < configValues.Length; i++)
                    if (MusicConstants.ConfigIDs[i] != -1 && configValues[i] != 0)
                        _owner.Configurations.SendStandardConfiguration(MusicConstants.ConfigIDs[i], configValues[i]);
            }
            catch (IndexOutOfRangeException)
            {
            }
        }

        /// <summary>
        /// Refreshes the play list.
        /// </summary>
        public void RefreshPlayList()
        {
            var configValues = new int[_playListConfigIDs.Length];
            for (var i = 0; i < configValues.Length; i++)
                configValues[i] = -1;
            for (var i = 0; i < _playList.Count; i++)
            {
                var configValue = _playList[i++];
                if (i < _playList.Count)
                    configValue |= _playList[i] << 15;
                else
                    configValue |= -1 << 15;
                configValues[i / 2] = configValue;
            }

            for (var i = 0; i < _playListConfigIDs.Length; i++)
                _owner.Configurations.SendStandardConfiguration(_playListConfigIDs[i], configValues[i]);
        }

        /// <summary>
        /// Refreshes the play list toggle.
        /// </summary>
        public void RefreshPlayListToggle() => _owner.Configurations.SendBitConfiguration(7078, IsPlayListToggled ? 1 : 0);

        /// <summary>
        /// Refreshes the shuffle.
        /// </summary>
        public void RefreshShuffle() => _owner.Configurations.SendBitConfiguration(7079, IsShuffleToggled ? 1 : 0);

        public void Hydrate(HydratedMusicDto hydration)
        {
            _unlockedMusic.AddRange(hydration.UnlockedMusicIds);
            _playList = [..hydration.PlaylistMusicIds];
            IsPlayListToggled = hydration.IsPlaylistToggled;
            IsShuffleToggled = hydration.IsShuffleToggled;
        }

        public HydratedMusicDto Dehydrate() => new HydratedMusicDto(_unlockedMusic.ToArray(), _playList.ToArray(), IsPlayListToggled, IsShuffleToggled);
    }
}
