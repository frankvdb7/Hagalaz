using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Godwars
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData([11702, 11704, 11706, 11708])]
    public class GodSwordHilt : ItemScript
    {
        private readonly IItemBuilder _itemBuilder;

        public GodSwordHilt(IItemBuilder itemBuilder)
        {
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Uses the item on an other item.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedWith">The used with.</param>
        /// <param name="character">The character.</param>
        /// <returns>
        ///     <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public override bool UseItemOnItem(IItem used, IItem usedWith, ICharacter character)
        {
            var resultId = (used.Id, usedWith.Id) switch
            {
                (11702, 11690) or (11690, 11702) => 11694,
                (11708, 11690) or (11690, 11708) => 11700,
                (11706, 11690) or (11690, 11706) => 11698,
                (11704, 11690) or (11690, 11704) => 11696,
                _ => 0
            };

            return resultId != 0 && GodSwordAssembly.TryAssemble(_itemBuilder, used, usedWith, character, resultId);
        }
    }
}
