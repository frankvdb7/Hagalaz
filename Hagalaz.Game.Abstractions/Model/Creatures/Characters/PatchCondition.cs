using System;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines a set of flags representing the various conditions a farming patch can be in.
    /// </summary>
    [Flags]
    public enum PatchCondition : int
    {
        /// <summary>
        /// The patch is diseased.
        /// </summary>
        Diseased = 1,
        /// <summary>
        /// The patch has been watered.
        /// </summary>
        Watered = 1 << 1,
        /// <summary>
        /// The plant in the patch has died.
        /// </summary>
        Dead = 1 << 2,
        /// <summary>
        /// The patch has been treated with regular compost.
        /// </summary>
        Fertilized = 1 << 3,
        /// <summary>
        /// The patch has been treated with supercompost.
        /// </summary>
        SuperFertilized = 1 << 4,
        /// <summary>
        /// The player has checked the health of the fully grown plant.
        /// </summary>
        Checked = 1 << 5,
        /// <summary>
        /// The patch is empty and has weeds.
        /// </summary>
        Empty = 1 << 6,
        /// <summary>
        /// The patch has been fully raked and is clear of weeds.
        /// </summary>
        Cleared = 1 << 7,
        /// <summary>
        /// The plant in the patch is fully grown and ready for harvest.
        /// </summary>
        Mature = 1 << 8,
        /// <summary>
        /// The patch has a seed planted in it.
        /// </summary>
        Planted = 1 << 9,
    }

}
