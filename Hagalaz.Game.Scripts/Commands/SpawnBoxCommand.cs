using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Items;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Commands
{
    public class SpawnBoxCommand : IGameCommand
    {
        private readonly IItemBuilder _itemBuilder;

        public SpawnBoxCommand(IItemBuilder itemBuilder)
        {
            _itemBuilder = itemBuilder;
        }

        public string Name { get; } = "spawnbox";
        public Permission Permission { get; } = Permission.GameAdministrator;

        public async Task Execute(GameCommandArgs args)
        {
            await Task.CompletedTask;
            if (args.Character.Area.IsPvP)
            {
                args.Character.SendChatMessage("You can't use this command in PvP zone.");
                return;
            }

            if (args.Arguments.Length <= 0)
            {
                args.Character.SendChatMessage("No item name entered.");
                return;
            }

            var itemPart = string.Join(" ", args.Arguments).ToLower();
            var itemRepository = args.Character.ServiceProvider.GetRequiredService<IItemService>();

            var count = itemRepository.GetTotalItemCount();
            var found = new int[8 * 35];
            var foundCount = 0;
            _ = Task.Run(() =>
            {
                for (var i = 0; i < count; i++)
                {
                    if (!itemRepository.FindItemDefinitionById(i).Name.Contains(itemPart, StringComparison.CurrentCultureIgnoreCase))
                    {
                        continue;
                    }

                    found[foundCount++] = i;
                    if (foundCount < found.Length)
                    {
                        continue;
                    }

                    args.Character.SendChatMessage("Too much results, please enter more accurate name.");
                    return;
                }

                var defaultScript = args.Character.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
                args.Character.Widgets.OpenWidget(645, 0, defaultScript, true);
                var spawnBox = args.Character.Widgets.GetOpenWidget(645);
                if (spawnBox == null)
                {
                    args.Character.SendChatMessage("Could not open spawn box.");
                    return;
                }

                // setupInterfaceItemsDisplayFromItemsArrayNonSplit(icomponent,itemsArrayIndex,numRows,numCollumns,dragOptions,dragTarget,option1,option2,option3,option4,option5,option6,option7,option8,option9) : 150
                args.Character.Configurations.SendCs2Script(150,
                [
                    (645 << 16) | 16, 90, 8, 35, 0, -1, "Take", "Take-X", "", "", "", "", "", "", ""
                ]);
                spawnBox.SetOptions(16, 0, 8 * 35 - 1, 0x2 | 0x4); // allow 2 options
                spawnBox.DrawString(15, "Spawn Box");
                spawnBox.DrawString(17, "Found <col=FFFF>" + foundCount + "</col>" + " items for:<br><col=00FFFF>" + itemPart + "</col>");
                //"Item results for:<br><col=FFFFFF>" + itemPart + "</col>");
                spawnBox.SetVisible(19, false); // disable the collect sprite
                if (foundCount <= 48)
                {
                    spawnBox.SetVisible(18, false); // disable scroll bar
                }


                var container = new GenericContainer(StorageType.AlwaysStack, 8 * 35);
                for (var i = 0; i < foundCount; i++)
                {
                    container.Add(_itemBuilder.Create().WithId(found[i]).Build());
                }

                args.Character.Configurations.SendItems(90, false, container);

                spawnBox.AttachClickHandler(16,
                    (componentID, clickType, itemID, slot) =>
                    {
                        if (clickType == ComponentClickType.LeftClick)
                        {
                            if (slot < 0 || slot >= container.Capacity)
                            {
                                return false;
                            }

                            var item = container[slot];
                            if (item == null || item.Id != itemID)
                            {
                                return false;
                            }

                            if (args.Character.Inventory.HasSpaceFor(item))
                            {
                                args.Character.Inventory.Add(item.Clone());
                            }
                            else
                            {
                                args.Character.SendChatMessage("Not enough space in your inventory.");
                            }

                            return true;
                        }

                        if (clickType == ComponentClickType.Option2Click)
                        {
                            var inputHandler = args.Character.Widgets.IntInputHandler = value =>
                            {
                                args.Character.Widgets.IntInputHandler = null;
                                if (!spawnBox.IsOpened)
                                {
                                    return;
                                }

                                if (value <= 0)
                                {
                                    args.Character.SendChatMessage("Amount must be greater than 0.");
                                }

                                var item = container[slot];
                                if (item == null || item.Id != itemID)
                                {
                                    return;
                                }

                                if (!item.ItemDefinition.Stackable && !item.ItemDefinition.Noted)
                                {
                                    args.Character.SendChatMessage("You can only do this on stackable items only!");
                                    return;
                                }

                                var addItem = item.Clone();
                                addItem.Count = value;

                                if (args.Character.Inventory.HasSpaceFor(addItem))
                                {
                                    args.Character.Inventory.Add(addItem);
                                }
                                else
                                {
                                    args.Character.SendChatMessage("Not enough space in your inventory.");
                                }
                            };
                            args.Character.Configurations.SendIntegerInput("Please enter the amount to take:");
                            return true;
                        }

                        return false;
                    });
            });
        }
    }
}