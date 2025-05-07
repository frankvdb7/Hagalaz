using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects.Altars
{
    [GameObjectScriptMetaData([409, 27661])]
    public class StandardAltar : GameObjectScript
    {
        private readonly IItemBuilder _itemBuilder;

        public StandardAltar(IItemBuilder itemBuilder) => _itemBuilder = itemBuilder;

        /// <summary>
        ///     Happens when character click's this object and then walks to it
        ///     and reaches it.
        ///     This method is called by OnCharacterClick by default, if OnCharacterClick is overrided
        ///     than this method is not guaranteed to be called.
        /// </summary>
        /// <param name="clicker">Character which clicked on the object.</param>
        /// <param name="clickType">Type of the click that was performed.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option1Click)
            {
                if (clicker.Statistics.HealPrayerPoints(990) != 0)
                {
                    clicker.QueueAnimation(Animation.Create(645));
                    clicker.SendChatMessage("You prayed to the gods and they restored your prayer points.");
                }
                else
                {
                    clicker.SendChatMessage("You cannot restore any more prayer points.");
                }

                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Uses the item on game object.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool UseItemOnGameObject(IItem used, ICharacter character)
        {
            if (used.Id == 13754 // holy elixer
                || used.Id == 13734) // normal spirit shield
            {
                var blessedShield = _itemBuilder.Create().WithId(13736).Build();
                if (character.Statistics.GetSkillLevel(StatisticsConstants.Prayer) < 85)
                {
                    character.SendChatMessage("You need a prayer level of 85 in order to create a " + blessedShield.Name);
                    return false;
                }

                var usedSlot = character.Inventory.GetInstanceSlot(used);
                if (usedSlot == -1) // no instanc of this item exists, possibly a rogue item or hack
                {
                    return false;
                }

                var otherItem = _itemBuilder.Create().WithId(used.Id == 13754 ? 13734 : 13754).Build();
                var otherSlot = character.Inventory.GetSlotByItem(otherItem);
                if (otherSlot == -1)
                {
                    character.SendChatMessage("You need a " + otherItem.Name + " in order to create a " + blessedShield.Name);
                    return false;
                }

                var removed = character.Inventory.Remove(used, usedSlot);
                removed += character.Inventory.Remove(otherItem, otherSlot);
                if (removed == 2)
                {
                    character.Inventory.Add(blessedShield);
                    return true;
                }

                return false;
            }

            character.SendChatMessage("You fear the wrath of the gods!");
            return false;
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}