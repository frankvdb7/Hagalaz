using System;
using Hagalaz.Game.Abstractions.Model.Creatures;

namespace Hagalaz.Services.GameWorld.Model.Creatures
{
    /// <summary>
    /// Class representing bonuses.
    /// </summary>
    public readonly struct Bonuses : IBonuses
    {
        /// <summary>
        /// Contains bonuses.
        /// </summary>
        private readonly int[] _bonuses = new int[18];

        /// <summary>
        /// Construct's new bonuses class.
        /// </summary>
        public Bonuses()
        {
        }

        /// <summary>
        /// Construc's new bonuses class.
        /// </summary>
        /// <param name="bonuses"></param>
        public Bonuses(int[] bonuses)
        {
            for (var i = 0; i < bonuses.Length; i++)
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
        public void Add(IBonuses<BonusType> bonuses)
        {
            for (int i = 0; i < _bonuses.Length; i++)
                _bonuses[i] += bonuses.GetBonus((BonusType)i);
        }

        /// <summary>
        /// Remove's specific bonuses to this bonuses class.
        /// </summary>
        /// <param name="bonuses">Bonuses which should be removed.</param>
        public void Remove(IBonuses<BonusType> bonuses)
        {
            for (int i = 0; i < _bonuses.Length; i++)
                _bonuses[i] -= bonuses.GetBonus((BonusType)i);
        }

        /// <summary>
        /// Get's specific bonus.
        /// </summary>
        /// <param name="type">Type of the bonus to get.</param>
        /// <returns></returns>
        public int GetBonus(BonusType type)
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
        public void SetBonus(BonusType type, int value)
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
        public IBonuses Copy() => new Bonuses(_bonuses);
    }
}