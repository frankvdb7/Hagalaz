using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Currencies
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData([1815])]
    public class PvPToken : ItemScript
    {
        /// <summary>
        ///     Get's if specific item can be sold to the specified shop.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool CanSellItem(IItem item, ICharacter character) => false;

        /// <summary>
        ///     Gets the examine.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override string GetExamine(IItem item)
        {
            if (item.Count >= 100000)
            {
                return item.Count + " x Tokens";
            }

            return "Lovely tokens!";
        }

        /// <summary>
        ///     Determines whether this instance [can stack item] the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="otherItem">The other item.</param>
        /// <param name="containerAlwaysStack">if set to <c>true</c> [container always stack].</param>
        /// <returns></returns>
        public override bool CanStackItem(IItem item, IItem otherItem, bool containerAlwaysStack)
        {
            if (item.Id == otherItem.Id)
            {
                if (item.ExtraData.Length != otherItem.ExtraData.Length)
                {
                    return false;
                }

                for (var i = 0; i < item.ExtraData.Length; i++)
                {
                    if (item.ExtraData[i] != otherItem.ExtraData[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}