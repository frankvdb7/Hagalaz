using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects
{
    [GameObjectScriptMetaData([2646])]
    public class Flax : GameObjectScript
    {
        private readonly IRsTaskService _taskService;
        private readonly IItemBuilder _itemBuilder;

        public Flax(IRsTaskService taskService, IItemBuilder itemBuilder)
        {
            _taskService = taskService;
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Called when [character click perform].
        /// </summary>
        /// <param name="clicker">The clicker.</param>
        /// <param name="clickType">Type of the click.</param>
        public override void OnCharacterClickPerform(ICharacter clicker, GameObjectClickType clickType)
        {
            if (clickType == GameObjectClickType.Option2Click)
            {
                clicker.Interrupt(this);
                if (clicker.Inventory.FreeSlots <= 0)
                {
                    clicker.SendChatMessage(GameStrings.InventoryFull);
                    return;
                }

                clicker.Movement.Lock(true);
                clicker.QueueAnimation(Animation.Create(827));
                clicker.QueueTask(new RsTask(() =>
                    {
                        if (clicker.Inventory.Add(_itemBuilder.Create().WithId(1779).Build()))
                        {
                            if (0.40 >= RandomStatic.Generator.NextDouble())
                            {
                                // delete the flax object.
                                Owner.Region.Remove(Owner);
                                _taskService.Schedule(new RsTask(() => Owner.Region.Add(Owner), 8));
                            }

                            clicker.SendChatMessage("You picked up the flax from the ground.");
                        }
                        else
                        {
                            clicker.SendChatMessage(GameStrings.InventoryFull);
                        }

                        clicker.Movement.Unlock(false);
                    },
                    2));
                return;
            }

            base.OnCharacterClickPerform(clicker, clickType);
        }

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize() { }
    }
}