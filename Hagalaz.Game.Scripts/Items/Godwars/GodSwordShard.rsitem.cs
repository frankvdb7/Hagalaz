using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.Items.Godwars
{
    /// <summary>
    /// </summary>
    [ItemScriptMetaData([11686, 11688, 11690, 11692, 11710, 11712, 11714])]
    public class GodSwordShard : ItemScript
    {
        private readonly IItemBuilder _itemBuilder;

        public GodSwordShard(IItemBuilder itemBuilder)
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
                (11710, 11692) or (11692, 11710) => 11690,
                (11712, 11688) or (11688, 11712) => 11690,
                (11714, 11686) or (11686, 11714) => 11690,
                (11712, 11714) or (11714, 11712) => 11692,
                (11710, 11712) or (11712, 11710) => 11686,
                (11710, 11714) => 11688,
                _ => 0
            };

            if (resultId != 0 && GodSwordAssembly.TryAssemble(_itemBuilder, used, usedWith, character, resultId))
                return true;
            return base.UseItemOnItem(used, usedWith, character);
        }
    }
}
