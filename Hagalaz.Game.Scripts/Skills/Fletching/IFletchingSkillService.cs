using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Fletching
{
    public interface IFletchingSkillService
    {
        /// <summary>
        ///     Tries the make strung bow.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The item.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        bool TryFletchWood(ICharacter character, IItem used, IItem usedWith);

        /// <summary>
        ///     Tries the fletch a bow (u) to a bow.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        bool TryFletchBow(ICharacter character, IItem used, IItem usedWith);

        /// <summary>
        ///     Tries the fletch a bow (u) to a bow.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        bool TryFletchAmmo(ICharacter character, IItem used, IItem usedWith);

        /// <summary>
        ///     Tries the fletch tips.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <returns></returns>
        bool TryFletchTips(ICharacter character, IItem used, IItem usedWith);

        /// <summary>
        ///     Tries to start fletching.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="definition">The definition.</param>
        /// <param name="onFletchingPerformCallback">The on fletching perform callback.</param>
        /// <param name="productIndex">Index of the product.</param>
        /// <param name="tickDelay">The tick delay.</param>
        /// <param name="count">The count.</param>
        void TryStartFletching(
            ICharacter character, FletchingDefinition definition, Func<int, bool> onFletchingPerformCallback, int productIndex, int tickDelay, int count);
    }
}