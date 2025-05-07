using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects
{
    [GameObjectScriptMetaData([1161])]
    public class Cabbage : GameObjectScript
    {
        private readonly IRsTaskService _rsTaskService;
        private readonly IItemBuilder _itemBuilder;

        public Cabbage(IRsTaskService rsTaskService, IItemBuilder itemBuilder)
        {
            _rsTaskService = rsTaskService;
            _itemBuilder = itemBuilder;
        }

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
            if (clickType == GameObjectClickType.Option2Click)
            {
                clicker.Interrupt(this);
                clicker.Movement.Lock(true);
                clicker.QueueAnimation(Animation.Create(827));
                clicker.SendChatMessage("You tried pulling the cabbage from the ground...");
                clicker.QueueTask(new RsTask(() =>
                    {
                        if (clicker.Inventory.Add(_itemBuilder.Create().WithId(1965).Build()))
                        {
                            // delete the cabbage object.
                            Owner.Region.Remove(Owner);
                            _rsTaskService.Schedule(new RsTask(() => Owner.Region.Add(Owner), 100));
                            clicker.SendChatMessage("You pulled the cabbage out of the ground.");
                        }
                        else
                        {
                            clicker.SendChatMessage(GameStrings.InventoryFull);
                        }

                        clicker.Movement.Unlock(false);
                    }, 2));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Get's called when owner is found.
        /// </summary>
        protected override void Initialize()
        {
        }
    }
}