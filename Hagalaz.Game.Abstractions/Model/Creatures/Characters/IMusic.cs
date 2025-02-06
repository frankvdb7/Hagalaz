namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMusic
    {
        /// <summary>
        /// contains the index of the playing music.
        /// </summary>
        int PlayingMusicIndex { get; }

        /// <summary>
        /// Plays music by id.
        /// </summary>
        /// <param name="musicIndex">Index of the music.</param>
        void PlayMusic(int musicIndex);

        /// <summary>
        /// Called when [music played].
        /// </summary>
        /// <param name="musicID">The music identifier.</param>
        /// <returns></returns>
        void OnMusicPlayed(int musicID);
        /// <summary>
        /// Unlocks the music.
        /// </summary>
        /// <param name="musicIndex">Index of the music.</param>
        /// <returns></returns>
        bool UnlockMusic(int musicIndex);
        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        void RefreshMusicList();
        /// <summary>
        /// Sends the music hint by the music index.
        /// </summary>
        /// <param name="musicIndex">The index.</param>
        void SendHint(int musicIndex);

        /// <summary>
        /// Adds the music to play list.
        /// </summary>
        /// <param name="musicIndex">Index of the music.</param>
        void AddMusicToPlayList(int musicIndex);
        /// <summary>
        /// Adds the playing music to play list.
        /// </summary>
        void AddPlayingMusicToPlayList();
        /// <summary>
        /// Removes the music from play list.
        /// </summary>
        /// <param name="musicIndex">Index of the music.</param>
        void RemoveMusicFromPlayList(int musicIndex);

        /// <summary>
        /// Plays the music from play list.
        /// </summary>
        /// <param name="playListIndex">Index of the play list.</param>
        void PlayMusicFromPlayList(int playListIndex);
        /// <summary>
        /// Removes the index of the music from play list by.
        /// </summary>
        /// <param name="playListIndex">Index of the play list.</param>
        void RemoveMusicFromPlayListByIndex(int playListIndex);
        /// <summary>
        /// Clears the play list.
        /// </summary>
        void ClearPlayList();
        /// <summary>
        /// Toggles the play list.
        /// </summary>
        void TogglePlayList();
        /// <summary>
        /// Toggles the shuffle.
        /// </summary>
        void ToggleShuffle();
        /// <summary>
        /// Refreshes this instance.
        /// </summary>
        void Refresh();
        /// <summary>
        /// Gets the name of the music.
        /// </summary>
        /// <param name="musicIndex">Index of the music.</param>
        /// <returns></returns>
        string GetMusicName(int musicIndex);
    }
}
