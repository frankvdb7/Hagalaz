using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum PatchCondition : int
    {
        /// <summary>
        /// The diseased
        /// </summary>
        Diseased = 1,
        /// <summary>
        /// The watered
        /// </summary>
        Watered = 1 << 1,
        /// <summary>
        /// The dead
        /// </summary>
        Dead = 1 << 2,
        /// <summary>
        /// The fertilized
        /// </summary>
        Fertilized = 1 << 3,
        /// <summary>
        /// The super fertilized
        /// </summary>
        SuperFertilized = 1 << 4,
        /// <summary>
        /// The checked
        /// </summary>
        Checked = 1 << 5,
        /// <summary>
        /// The empty
        /// </summary>
        Empty = 1 << 6,
        /// <summary>
        /// Condition when a farming patch is fully raked.
        /// </summary>
        Cleared = 1 << 7,
        /// <summary>
        /// Condition when a farming patch is fully matured.
        /// </summary>
        Mature = 1 << 8,
        /// <summary>
        /// Condition when a farming patch has planted seeds.
        /// </summary>
        Planted = 1 << 9,
    }

}
