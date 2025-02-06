using System;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Providers;

namespace Hagalaz.Services.GameWorld.Providers
{
    /// <summary>
    /// Contains some functions for calculating hit type.
    /// </summary>
    public class HitSplatRenderTypeProvider : IHitSplatRenderTypeProvider
    {
        /// <summary>
        /// Contains hit types in format _hitTypes[hittype,0:1,0:1]
        /// </summary>
        /// <value>The hit types.</value>
        private readonly int[,,] _hitTypes;

        /// <summary>
        /// 
        /// </summary>
        public HitSplatRenderTypeProvider()
        {
            _hitTypes = new int[11, 2, 2];

            Add(0, 8, 8, 8, 8);
            Add(1, 0, 14, 10, 24);
            Add(2, 1, 15, 11, 25);
            Add(3, 2, 16, 12, 26);
            Add(4, 3, 17, 3, 17);
            Add(5, 6, 20, 6, 20);
            Add(6, 7, 7, 7, 7);
            Add(7, 9, 23, 9, 23);
            Add(8, 4, 18, 4, 18);
            Add(9, 13, 27, 13, 27);
            Add(10, 5, 19, 5, 19);
        }

        /// <summary>
        /// Add's hit to hit types.
        /// </summary>
        /// <param name="type">Type of the hit.</param>
        /// <param name="active">Active render Id of the specific hit type.</param>
        /// <param name="unactive">Inactype render Id of the specific hit type.</param>
        /// <param name="criticalDamageActive">Active maxed render Id of the specific hit type.</param>
        /// <param name="criticalDamageNotActive">Inactive maxed render Id of the specific hit type.</param>
        private void Add(int type, int active, int unactive, int criticalDamageActive, int criticalDamageNotActive)
        {
            _hitTypes[type, 0, 0] = criticalDamageActive;
            _hitTypes[type, 0, 1] = active;
            _hitTypes[type, 1, 0] = criticalDamageNotActive;
            _hitTypes[type, 1, 1] = unactive;
        }

        /// <summary>
        /// Get's hit render type.
        /// </summary>
        /// <param name="splatType">Type of the hit splat.</param>
        /// <param name="active">Whether damage is active.</param>
        /// <param name="isCriticalDamage">Whether hit is maxed.</param>
        /// <returns>HitSplatRenderType.</returns>
        /// <exception cref="Exception"></exception>
        public HitSplatRenderType GetRenderType(HitSplatType splatType, bool active, bool isCriticalDamage)
        {
            if (splatType == HitSplatType.None)
                throw new Exception("HitSplatType is not suitable for GetRenderType()");
            return (HitSplatRenderType)_hitTypes[(int)splatType, active ? 0 : 1, isCriticalDamage ? 0 : 1];
        }
    }
}