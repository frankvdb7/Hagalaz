namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for managing a character's music player, including unlocked tracks, playlists, and playback state.
    /// </summary>
    public interface IMusic
    {
        /// <summary>
        /// Gets the index of the music track that is currently playing.
        /// </summary>
        int PlayingMusicIndex { get; }

        /// <summary>
        /// Plays a specific music track by its index.
        /// </summary>
        /// <param name="musicIndex">The index of the music track to play.</param>
        void PlayMusic(int musicIndex);

        /// <summary>
        /// A callback executed when a music track has finished playing.
        /// </summary>
        /// <param name="musicID">The ID of the music track that finished.</param>
        void OnMusicPlayed(int musicID);

        /// <summary>
        /// Unlocks a specific music track for the character, making it available in their music list.
        /// </summary>
        /// <param name="musicIndex">The index of the music track to unlock.</param>
        /// <returns><c>true</c> if the track was successfully unlocked; otherwise, <c>false</c>.</returns>
        bool UnlockMusic(int musicIndex);

        /// <summary>
        /// Sends an update to the client to refresh the entire music list interface.
        /// </summary>
        void RefreshMusicList();

        /// <summary>
        /// Sends a hint to the client about how to unlock a specific music track.
        /// </summary>
        /// <param name="musicIndex">The index of the music track.</param>
        void SendHint(int musicIndex);

        /// <summary>
        /// Adds a music track to the character's custom playlist.
        /// </summary>
        /// <param name="musicIndex">The index of the music track to add.</param>
        void AddMusicToPlayList(int musicIndex);

        /// <summary>
        /// Adds the currently playing music track to the custom playlist.
        /// </summary>
        void AddPlayingMusicToPlayList();

        /// <summary>
        /// Removes a music track from the custom playlist by its track index.
        /// </summary>
        /// <param name="musicIndex">The index of the music track to remove.</param>
        void RemoveMusicFromPlayList(int musicIndex);

        /// <summary>
        /// Plays a music track from a specific position in the custom playlist.
        /// </summary>
        /// <param name="playListIndex">The index within the playlist to play from.</param>
        void PlayMusicFromPlayList(int playListIndex);

        /// <summary>
        /// Removes a music track from the custom playlist by its position in the list.
        /// </summary>
        /// <param name="playListIndex">The index within the playlist to remove.</param>
        void RemoveMusicFromPlayListByIndex(int playListIndex);

        /// <summary>
        /// Clears all tracks from the custom playlist.
        /// </summary>
        void ClearPlayList();

        /// <summary>
        /// Toggles the playlist loop functionality on or off.
        /// </summary>
        void TogglePlayList();

        /// <summary>
        /// Toggles the shuffle functionality for the playlist on or off.
        /// </summary>
        void ToggleShuffle();

        /// <summary>
        /// Sends an update to the client to refresh the state of the music player (e.g., volume, shuffle/loop status).
        /// </summary>
        void Refresh();

        /// <summary>
        /// Gets the name of a music track by its index.
        /// </summary>
        /// <param name="musicIndex">The index of the music track.</param>
        /// <returns>The name of the music track.</returns>
        string GetMusicName(int musicIndex);
    }
}