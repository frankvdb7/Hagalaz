using System;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Items
{
    /// <summary>
    /// </summary>
    public enum ColouringItems : short
    {
        /// <summary>
        ///     The none
        /// </summary>
        None = 0,

        /// <summary>
        ///     The red dye
        /// </summary>
        RedDye = 1763,

        /// <summary>
        ///     The yellow dye
        /// </summary>
        YellowDye = 1765,

        /// <summary>
        ///     The blue dye
        /// </summary>
        BlueDye = 1767,

        /// <summary>
        ///     The orange dye
        /// </summary>
        OrangeDye = 1769,

        /// <summary>
        ///     The green dye
        /// </summary>
        GreenDye = 1771,

        /// <summary>
        ///     The purple dye
        /// </summary>
        PurpleDye = 1773,

        /// <summary>
        ///     The white fish
        /// </summary>
        WhiteFish = 11808,

        /// <summary>
        ///     The cleaning cloth
        /// </summary>
        CleaningCloth = 3188
    }

    /// <summary>
    /// </summary>
    public static class ColourMethods
    {
        /// <summary>
        ///     Gets the colouring item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public static ColouringItems GetColouringItem(IItem item)
        {
            foreach (ColouringItems colour in Enum.GetValues(typeof(ColouringItems)))
            {
                if ((short)colour == item.Id)
                {
                    return colour;
                }
            }

            return ColouringItems.None;
        }
    }
}