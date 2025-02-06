using System;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    /// <summary>
    /// Class represending prayer bonuses.
    /// </summary>
    public readonly struct PrayerBonuses : IBonusesPrayer
    {
        /// <summary>
        /// Contains bonuses.
        /// </summary>
        private readonly int[] _bonuses = new int[18];

        /// <summary>
        /// Construct's new bonuses class.
        /// </summary>
        public PrayerBonuses()
        {
        }

        /// <summary>
        /// Construc's new bonuses class.
        /// </summary>
        /// <param name="bonuses"></param>
        public PrayerBonuses(int[] bonuses)
        {
            for (int i = 0; i < bonuses.Length; i++)
            {
                if (i >= _bonuses.Length)
                    break;
                _bonuses[i] = bonuses[i];
            }
        }

        /// <summary>
        /// Add's specific bonuses to this bonuses class.
        /// </summary>
        /// <param name="bonuses">Bonuses which should be added.</param>
        public void Add(IBonuses<BonusPrayerType> bonuses)
        {
            for (int i = 0; i < _bonuses.Length; i++)
                _bonuses[i] += bonuses.GetBonus((BonusPrayerType)i);
        }

        /// <summary>
        /// Remove's specific bonuses to this bonuses class.
        /// </summary>
        /// <param name="bonuses">Bonuses which should be removed.</param>
        public void Remove(IBonuses<BonusPrayerType> bonuses)
        {
            for (int i = 0; i < _bonuses.Length; i++)
                _bonuses[i] -= bonuses.GetBonus((BonusPrayerType)i);
        }

        /// <summary>
        /// Add's to specific bonus.
        /// </summary>
        /// <param name="type">Type of the bonus to add to.</param>
        /// <param name="amount">Amount which should be added.</param>
        public void AddToBonus(BonusPrayerType type, int amount)
        {
            if ((int)type < 0 || (int)type >= _bonuses.Length)
                throw new Exception("Invalid bonus type!");
            _bonuses[(int)type] += amount;
        }

        /// <summary>
        /// Remove's from specific bonus.
        /// </summary>
        /// <param name="type">Type of the bonus to remove from.</param>
        /// <param name="amount">Amount which should be removed.</param>
        public void RemoveFromBonus(BonusPrayerType type, int amount)
        {
            if ((int)type < 0 || (int)type >= _bonuses.Length)
                throw new Exception("Invalid bonus type!");
            _bonuses[(int)type] -= amount;
        }

        /// <summary>
        /// Get's specific bonus.
        /// </summary>
        /// <param name="type">Type of the bonus to get.</param>
        /// <returns></returns>
        public int GetBonus(BonusPrayerType type)
        {
            if ((int)type < 0 || (int)type >= _bonuses.Length)
                throw new Exception("Invalid bonus type!");
            return _bonuses[(int)type];
        }

        /// <summary>
        /// Set's specific bonus.
        /// </summary>
        /// <param name="type">Type of the bonus to set.</param>
        /// <param name="value">Value which should be seted.</param>
        public void SetBonus(BonusPrayerType type, int value)
        {
            if ((int)type < 0 || (int)type >= _bonuses.Length)
                throw new Exception("Invalid bonus type!");
            _bonuses[(int)type] = value;
        }

        /// <summary>
        /// Set's all bonuses to 0.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < _bonuses.Length; i++)
                _bonuses[i] = 0;
        }

        /// <summary>
        /// Copies this bonuses.
        /// </summary>
        /// <returns></returns>
        public IBonusesPrayer Copy() => new PrayerBonuses(_bonuses);
    }
}