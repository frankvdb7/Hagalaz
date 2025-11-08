using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Model.Creatures.Npcs;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Npcs.Familiars;
using Hagalaz.Game.Scripts.Widgets.Tabs;

namespace Hagalaz.Game.Scripts.Widgets.Orbs
{
    /// <summary>
    ///     Represents summoning orb.
    /// </summary>
    public class SummoningOrb : WidgetScript
    {
        /// <summary>
        ///     The familiar spawned handler.
        /// </summary>
        private EventHappened _familiarSpawnedHandler = default!;

        /// <summary>
        ///     The familiar dismissed handler.
        /// </summary>
        private EventHappened _familiarDismissHandler = default!;

        /// <summary>
        ///     The interface openened handler.
        /// </summary>
        private EventHappened _interfaceOpenedHandler = default!;

        /// <summary>
        ///     The interface closed handler.
        /// </summary>
        private EventHappened _interfaceClosedHandler = default!;
        private readonly IScopedGameMediator _gameMediator;
        private readonly LeftClickOptionTab _leftClickOptionTab;

        public SummoningOrb(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator, LeftClickOptionTab leftClickOptionTab) : base(characterContextAccessor)
        {
            _gameMediator = gameMediator;
            _leftClickOptionTab = leftClickOptionTab;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            Owner.QueueTask(new RsTask(() => InterfaceInstance.SetVisible(9, Owner.HasFamiliar()), 1));
            _familiarSpawnedHandler = Owner.RegisterEventHandler<FamiliarSpawnedEvent>(OnFamiliarSpawned);
            _interfaceOpenedHandler = Owner.RegisterEventHandler(new EventHappened<InterfaceOpenedEvent>(e =>
            {
                if (e.Opened == InterfaceInstance)
                {
                    return false;
                }

                if (Owner.HasFamiliar())
                {
                    InterfaceInstance.SetVisible(9, false);
                }

                return false;
            }));
            _interfaceClosedHandler = Owner.RegisterEventHandler(new EventHappened<InterfaceClosedEvent>(e =>
            {
                if (e.Closed == InterfaceInstance)
                {
                    return false;
                }

                if (Owner.HasFamiliar())
                {
                    InterfaceInstance.SetVisible(9, true);
                }

                return false;
            }));

            // Select left click option
            InterfaceInstance.AttachClickHandler(8, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.Option10Click)
                {
                    return false;
                }

                SelectLeftClickOption();
                return true;

            });

            // Follower Details
            InterfaceInstance.AttachClickHandler(10, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.Option6Click)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                var summoningTab = Owner.ServiceProvider.GetRequiredService<SummoningTab>();
                Owner.Widgets.OpenWidget(662, Owner.GameClient.IsScreenFixed ? 187 : 162, 1, summoningTab, false);
                return true;
            });

            // Call Familiar
            InterfaceInstance.AttachClickHandler(11, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.Option6Click)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                Owner.FamiliarScript.CallFamiliar();
                return true;
            });

            // Dismiss
            InterfaceInstance.AttachClickHandler(12, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.Option6Click)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                Owner.EventManager.SendEvent(new FamiliarDismissEvent(Owner));
                return true;
            });

            // Take BoB
            InterfaceInstance.AttachClickHandler(13, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.Option6Click)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                if (Owner.FamiliarScript is not BobFamiliarScriptBase bob)
                {
                    return false;
                }

                Owner.Inventory.AddAndRemoveFrom(bob.Inventory);
                return true;
            });

            // Renew FamiliarScript
            InterfaceInstance.AttachClickHandler(14, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.Option6Click)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                Owner.FamiliarScript.RenewFamiliar();
                return true;
            });

            // Interact
            InterfaceInstance.AttachClickHandler(16, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.Option6Click)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                if (Owner.FamiliarScript.Summoner != Owner)
                {
                    return false;
                }

                var familiarDialogue = Owner.ServiceProvider.GetRequiredService<StandardFamiliarDialogue>();
                Owner.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.Send2TextChatRight, 0, familiarDialogue, true, Owner.FamiliarScript.Familiar);
                return true;
            });

            // Special move
            InterfaceInstance.AttachClickHandler(18, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                Owner.FamiliarScript.SetSpecialMoveTarget(null);
                return true;
            });

            // Left click FamiliarScript details
            InterfaceInstance.AttachClickHandler(19, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                var summoningTab = Owner.ServiceProvider.GetRequiredService<SummoningTab>();
                Owner.Widgets.OpenWidget(662, Owner.GameClient.IsScreenFixed ? 187 : 119, 1, summoningTab, true);
                Owner.QueueTask(new RsTask(() => InterfaceInstance.SetVisible(9, Owner.HasFamiliar()), 1));
                return true;
            });

            // Left click call FamiliarScript
            InterfaceInstance.AttachClickHandler(20, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                Owner.FamiliarScript.CallFamiliar();
                return true;
            });

            // Left click dismiss FamiliarScript
            InterfaceInstance.AttachClickHandler(21, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                Owner.EventManager.SendEvent(new FamiliarDismissEvent(Owner));
                return true;
            });

            // Left click take BoB
            InterfaceInstance.AttachClickHandler(22, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                if (Owner.FamiliarScript is not BobFamiliarScriptBase bob)
                {
                    return false;
                }

                Owner.Inventory.AddAndRemoveFrom(bob.Inventory);
                return true;
            });

            // Left click renew FamiliarScript
            InterfaceInstance.AttachClickHandler(23, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                Owner.FamiliarScript.RenewFamiliar();
                return true;
            });

            // Left click Interact
            InterfaceInstance.AttachClickHandler(27, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                if (Owner.FamiliarScript.Summoner != Owner)
                {
                    return false;
                }

                var familiarDialogue = Owner.ServiceProvider.GetRequiredService<StandardFamiliarDialogue>();
                Owner.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.Send2TextChatRight, 0, familiarDialogue, true, Owner.FamiliarScript.Familiar);
                return true;
            });

            // Attack
            InterfaceInstance.AttachUseOnCreatureHandler(15, (componentID, creature, forceRun, extraData1, extraData2) =>
            {
                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                if (Owner.FamiliarScript.Familiar == creature)
                {
                    return false;
                }

                Owner.FamiliarScript.Familiar.QueueTask(new RsTask(() => Owner.FamiliarScript.Familiar.Combat.SetTarget(creature), 1));
                return true;
            });

            // Attack left click
            InterfaceInstance.AttachUseOnCreatureHandler(24, (componentID, creature, forceRun, extraData1, extraData2) =>
            {
                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                if (Owner.FamiliarScript.Familiar == creature)
                {
                    return false;
                }

                Owner.FamiliarScript.Familiar.QueueTask(new RsTask(() => Owner.FamiliarScript.Familiar.Combat.SetTarget(creature), 1));
                return true;
            });

            // Cast special move
            InterfaceInstance.AttachUseOnCreatureHandler(18, (componentID, creature, forceRun, extraData1, extraData2) =>
            {
                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                if (Owner.FamiliarScript.Familiar == creature)
                {
                    return false;
                }

                Owner.FamiliarScript.SetSpecialMoveTarget(creature);
                return true;
            });

            // Cast special move
            InterfaceInstance.AttachUseOnComponentHandler(18, (componentID, itemUsedOnId, itemUsedOnSlot, extra1, extra2) =>
            {
                if (!Owner.HasFamiliar())
                {
                    return false;
                }

                if (itemUsedOnSlot < 0 || itemUsedOnSlot >= Owner.Inventory.Capacity)
                {
                    return false;
                }

                var item = Owner.Inventory[itemUsedOnSlot];
                if (item == null || item.Id != itemUsedOnId)
                {
                    return false;
                }

                Owner.FamiliarScript.SetSpecialMoveTarget(item);
                return true;
            });
        }

        /// <summary>
        ///     Called when [familiar dismissed].
        /// </summary>
        /// <param name="e">The e.</param>
        /// <returns></returns>
        private bool OnFamiliarDismissed(FamiliarDismissEvent e)
        {
            InterfaceInstance.SetVisible(9, false); // hide all right click options
            Owner.Configurations.SendStandardConfiguration(448, -1);
            Owner.Configurations.SendStandardConfiguration(1174, -1);
            InterfaceInstance.SetOptions(18, 0, 0, 0);
            Owner.Widgets.CloseWidget(Owner.Widgets.GetOpenWidget(880)); // left click selection
            Owner.Widgets.CloseWidget(Owner.Widgets.GetOpenWidget(662)); // familiar description
            Owner.Widgets.CloseWidget(Owner.Widgets.GetOpenWidget(671)); // bob interface
            return false;
        }

        /// <summary>
        ///     Called when [familiar spawned].
        /// </summary>
        /// <param name="e">The e.</param>
        /// <returns></returns>
        private bool OnFamiliarSpawned(FamiliarSpawnedEvent e)
        {
            var familiar = e.Familiar.GetScript<FamiliarScriptBase>();
            InterfaceInstance.SetVisible(9, true); // show all right click options
            Owner.Configurations.SendStandardConfiguration(1174, e.Familiar.Appearance.CompositeID); // Could be model id.
            Owner.Configurations.SendStandardConfiguration(448, familiar.Definition.PouchId); // Also highlights summoning orb
            Owner.Configurations.SendGlobalCs2String(204, familiar.GetSpecialMoveName()); // Special name
            Owner.Configurations.SendGlobalCs2String(205, familiar.GetSpecialMoveDescription()); // Special description
            Owner.Configurations.SendGlobalCs2Int(1436, familiar.GetSpecialType() == FamiliarSpecialType.Click ? 1 : 0); // Special attack type

            var value = 0;
            if (Owner.HasFamiliar())
            {
                if (Owner.FamiliarScript.GetSpecialType() == FamiliarSpecialType.Click)
                {
                    value = 2;
                }
                else if (Owner.FamiliarScript.GetSpecialType() == FamiliarSpecialType.Creature)
                {
                    value = 20480;
                }
                else
                {
                    value = 65536;
                }
            }

            InterfaceInstance.SetOptions(18, 0, 0, value);

            _familiarDismissHandler = Owner.RegisterEventHandler<FamiliarDismissEvent>(OnFamiliarDismissed);
            return false;
        }

        /// <summary>
        ///     Selects the left click option.
        /// </summary>
        private void SelectLeftClickOption() => Owner.Widgets.OpenWidget(880, Owner.GameClient.IsScreenFixed ? 187 : 119, 1, _leftClickOptionTab, true);

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            //this.interfaceInstance.SetOptions(17, 0, 0, 0);
            if (_familiarDismissHandler != null)
            {
                Owner.UnregisterEventHandler<FamiliarDismissEvent>(_familiarDismissHandler);
            }

            if (_familiarSpawnedHandler != null)
            {
                Owner.UnregisterEventHandler<FamiliarSpawnedEvent>(_familiarSpawnedHandler);
            }

            if (_interfaceOpenedHandler != null)
            {
                Owner.UnregisterEventHandler<InterfaceOpenedEvent>(_interfaceOpenedHandler);
            }

            if (_interfaceClosedHandler != null)
            {
                Owner.UnregisterEventHandler<InterfaceClosedEvent>(_interfaceClosedHandler);
            }
        }
    }
}