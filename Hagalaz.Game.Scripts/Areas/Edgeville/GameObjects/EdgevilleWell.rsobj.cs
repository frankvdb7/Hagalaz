using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.GameObjects
{
    /// <summary>
    /// </summary>
    [GameObjectScriptMetaData([26945])]
    public class EdgevilleWell : GameObjectScript
    {
        private readonly IItemBuilder _itemBuilder;

        public EdgevilleWell(IItemBuilder itemBuilder)
        {
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Called when [use item].
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="used">The used.</param>
        /// <returns></returns>
        public override bool UseItemOnGameObject(IItem used, ICharacter character)
        {
            if (used.Id == 229)
            {
                var slot = character.Inventory.GetInstanceSlot(used);
                if (slot == -1)
                {
                    return false;
                }

                character.QueueAnimation(Animation.Create(827));
                character.Inventory.Replace(slot, _itemBuilder.Create().WithId(227).Build());
                character.SendChatMessage("You filled the vial with water from the well.");
                return true;
            }

            return false;
        }
    }
}