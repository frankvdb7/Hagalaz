using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Features.States.Effects;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Scripts.Model.Items;

namespace Hagalaz.Game.Scripts.GameObjects.Cannon
{
    [ItemScriptMetaData([6, 20494, 20498])]
    public class DwarfMultiCannonItemScript : ItemScript
    {
        private readonly IItemBuilder _itemBuilder;
        private readonly IGameObjectService _gameObjectService;

        /// <summary>
        ///     The cannon item ids.
        /// </summary>
        public static readonly int[] CannonItemIds = [6, 8, 10, 12, 20494, 20495, 20496, 20497, 20498, 20499, 20500, 20501];

        public DwarfMultiCannonItemScript(IItemBuilder itemBuilder, IGameObjectService gameObjectService)
        {
            _itemBuilder = itemBuilder;
            _gameObjectService = gameObjectService;
        }

        /// <summary>
        ///     Happens when specific item is clicked in specific character's inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">Item which was clicked in character's inventory.</param>
        /// <param name="character">Character which clicked on the item.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.LeftClick)
            {
                if (character.HasState<CannonPlacedState>())
                {
                    character.SendChatMessage("You have already placed a cannon base.");
                    return;
                }

                var cannonId = GetCannonIdByItem(item.Id);

                if (cannonId == -1)
                {
                    character.SendChatMessage("This cannon has not been added yet.");
                    return;
                }

                var pathFinder = character.ServiceProvider.GetRequiredService<ISimplePathFinder>();
                if (pathFinder.CheckStep(character.Location, 1))
                {
                    character.SendChatMessage("You can not place the cannon here!");
                    return;
                }

                var itemRepository = character.ServiceProvider.GetRequiredService<IItemService>();

                for (var i = cannonId; i < cannonId + 4; i++)
                {
                    if (character.Inventory.Contains(CannonItemIds[i]))
                    {
                        continue;
                    }

                    character.SendChatMessage("You need a " + itemRepository.FindItemDefinitionById(CannonItemIds[i]).Name +
                                              " in order to set up this cannon.");
                    return;
                }

                character.Movement.Lock(true);
                character.QueueAnimation(Animation.Create(827));

                character.AddState(new CannonPlacedState());

                var tick = 0;

                var gameObjectBuilder = character.ServiceProvider.GetRequiredService<IGameObjectBuilder>();
                var script = DwarfMultiCannonGameObjectScript.Create(character);
                var cannon = gameObjectBuilder.Create()
                    .WithId(DwarfMultiCannonGameObjectScript.CannonObjectIds[1 + cannonId])
                    .WithLocation(character.Location)
                    .WithScript(script)
                    .Build();
                RsTickTask task = null!;

                character.QueueTask(task = new RsTickTask(() =>
                {
                    if (tick == 0)
                    {
                        character.Region.Add(cannon);

                        character.SendChatMessage("You place the cannon base on the ground...");
                        character.Inventory.Remove(_itemBuilder.Create().WithId(CannonItemIds[0 + cannonId]).Build());
                    }
                    else if (tick == 2)
                    {
                        _gameObjectService.UpdateGameObject(new GameObjectUpdate
                        {
                            Instance = cannon,
                            Id = DwarfMultiCannonGameObjectScript.CannonObjectIds[2 + cannonId]
                        });
                        character.SendChatMessage("You add the stand...");
                        character.Inventory.Remove(_itemBuilder.Create().WithId(CannonItemIds[1 + cannonId]).Build());
                    }
                    else if (tick == 4)
                    {
                        _gameObjectService.UpdateGameObject(new GameObjectUpdate
                        {
                            Instance = cannon,
                            Id = DwarfMultiCannonGameObjectScript.CannonObjectIds[3 + cannonId]
                        });

                        character.SendChatMessage("You add the barrel...");
                        character.Inventory.Remove(_itemBuilder.Create().WithId(CannonItemIds[2 + cannonId]).Build());
                    }
                    else if (tick == 6)
                    {
                        _gameObjectService.UpdateGameObject(new GameObjectUpdate
                        {
                            Instance = cannon,
                            Id = DwarfMultiCannonGameObjectScript.CannonObjectIds[0 + cannonId]
                        });

                        character.SendChatMessage("You add the furnace...");
                        character.Inventory.Remove(_itemBuilder.Create().WithId(CannonItemIds[3 + cannonId]).Build());

                        character.Movement.Unlock(false);
                        task.Cancel();
                    }

                    character.QueueAnimation(Animation.Create(827));
                    tick++;
                }));
            }
            else
            {
                base.ItemClickedInInventory(clickType, item, character);
            }
        }

        /// <summary>
        ///     Gets the cannon id by itemId.
        /// </summary>
        /// <param name="itemId">The item id.</param>
        /// <returns></returns>
        private static int GetCannonIdByItem(int itemId)
        {
            if (itemId == CannonItemIds[0])
            {
                return 0; // Normal cannon
            }

            if (itemId == CannonItemIds[4])
            {
                return 4; // 4 - Gold cannon
            }

            if (itemId == CannonItemIds[8])
            {
                return 8; // 8 - Royal cannon
            }

            return -1;
        }
    }
}