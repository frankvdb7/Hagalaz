using Hagalaz.Services.Common.Data;

namespace Hagalaz.Services.Characters.Data
{
    public interface ICharacterUnitOfWork : IUnitOfWork
    {
        void Add<TEntity>(TEntity entity) where TEntity : class;
        void Remove<TEntity>(TEntity entity) where TEntity : class;

        /// <summary>
        /// Discards tracked changes after a failed persistence attempt so
        /// endpoint retries query fresh values from the database.
        /// </summary>
        void Reset();

        public ICharacterRepository CharacterRepository { get; }
        public ICharacterStatisticsRepository CharacterStatisticsRepository { get; }
        public ICharacterItemRepository CharacterItemRepository { get; }
        public ICharacterLookRepository CharacterLookRepository { get; }
        public ICharacterItemLookRepository CharacterItemLookRepository { get; }
        public ICharacterFamiliarRepository CharacterFamiliarRepository { get; }
        public ICharacterMusicRepository CharacterMusicRepository { get; }
        public ICharacterMusicPlaylistRepository CharacterMusicPlaylistRepository { get; }
        public ICharacterFarmingRepository CharacterFarmingRepository { get; }
        public ICharacterSlayerRepository CharacterSlayerRepository { get; }
        public ICharacterNotesRepository CharacterNotesRepository { get; }
        public ICharacterProfileRepository CharacterProfileRepository { get; }
        public ICharacterStateRepository CharacterStateRepository { get; }
    }
}
