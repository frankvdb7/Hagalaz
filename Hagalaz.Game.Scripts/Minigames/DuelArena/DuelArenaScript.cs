using System.Collections.Generic;
using Hagalaz.Collections.Extensions;
using Hagalaz.Game.Abstractions.Builders.HintIcon;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Items;
using Hagalaz.Game.Scripts.Minigames.DuelArena.Interfaces;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Minigames.DuelArena
{
    /// <summary>
    /// </summary>
    public class DuelArenaScript : CharacterScriptBase
    {
        private readonly IHintIconBuilder _hintIconBuilder;

        /// <summary>
        /// </summary>
        public enum DuelStage
        {
            /// <summary>
            ///     The request
            /// </summary>
            Request,

            /// <summary>
            ///     The first
            /// </summary>
            First,

            /// <summary>
            ///     The second
            /// </summary>
            Second
        }

        /// <summary>
        ///     The interfaces closed.
        /// </summary>
        private bool _interfacesClosed;

        /// <summary>
        ///     Contains last requested character.
        /// </summary>
        private ICharacter? LastRequest { get; set; }

        /// <summary>
        ///     Contains the stage.
        /// </summary>
        private DuelStage Stage { get; set; }

        /// <summary>
        ///     Contains the rules.
        /// </summary>
        public DuelRules CurrentRules { get; private set; }

        /// <summary>
        ///     Contains boolean if trade session is currently active.
        /// </summary>
        private bool DuelSession { get; set; }

        /// <summary>
        ///     Contains a value indicating whether this instance is staking.
        /// </summary>
        private bool IsStaking { get; set; }

        /// <summary>
        ///     Contains duel target.
        /// </summary>
        public ICharacter? Target { get; private set; }

        /// <summary>
        ///     Contains self interface.
        /// </summary>
        private IWidget? SelfInterface { get; set; }

        /// <summary>
        ///     Contains target interface.
        /// </summary>
        private IWidget? TargetInterface { get; set; }

        /// <summary>
        ///     Contains self overlay.
        /// </summary>
        private IWidget? SelfOverlay { get; set; }

        /// <summary>
        ///     Contains target overlay.
        /// </summary>
        private IWidget? TargetOverlay { get; set; }

        /// <summary>
        ///     Contains boolean if self player accepted.
        /// </summary>
        private bool SelfAccepted { get; set; }

        /// <summary>
        ///     Contains boolean if target player accepted.
        /// </summary>
        private bool TargetAccepted { get; set; }

        /// <summary>
        ///     Contains self int input handler.
        /// </summary>
        private OnIntInput? SelfIntInputHandler { get; set; }

        /// <summary>
        ///     Contains target int input handler.
        /// </summary>
        private OnIntInput? TargetIntInputHandler { get; set; }

        /// <summary>
        ///     Contains self container instance.
        /// </summary>
        private DuelContainer? SelfContainer { get; set; }

        /// <summary>
        ///     Contains target container instance.
        /// </summary>
        private DuelContainer? TargetContainer { get; set; }

        /// <summary>
        ///     Contains boolean if self trade was modified.
        /// </summary>
        private bool SelfModified { get; set; }

        /// <summary>
        ///     Contains boolean if target trade was modified.
        /// </summary>
        private bool TargetModified { get; set; }

        /// <summary>
        ///     Contains the encoded previous rules.
        /// </summary>
        private DuelRulesDto PreviousRules { get; set; }

        /// <summary>
        ///     Contains the encoded favourite rules.
        /// </summary>
        private DuelRulesDto FavoritedRules { get; set; }

        public DuelArenaScript(ICharacterContextAccessor contextAccessor, IHintIconBuilder hintIconBuilder)
            : base(contextAccessor)
        {
            _hintIconBuilder = hintIconBuilder;
        }

        /// <summary>
        ///     Initializes this script.
        /// </summary>
        protected override void Initialize()
        {
            FavoritedRules = Character.Profile.GetObject(DuelArenaConstants.MinigamesDuelArenaFavoriteRules,
                new DuelRulesDto
                {
                    Rules = []
                });
            PreviousRules = Character.Profile.GetObject(DuelArenaConstants.MinigamesDuelArenaPreviousRules,
                new DuelRulesDto
                {
                    Rules = []
                });
        }

        /// <summary>
        ///     Registers the character option handler.
        /// </summary>
        public void RegisterChallengeOptionHandler()
        {
            Stage = DuelStage.Request;
            Character.RegisterCharactersOptionHandler(CharacterClickType.Option1Click,
                "Challenge",
                65535,
                false,
                (target, forceRun) =>
                {
                    Character.Interrupt(this);
                    Character.ForceRunMovementType(forceRun);
                    var task = new CreatureReachTask(Character,
                        target,
                        success =>
                        {
                            Character.Interrupt(this);
                            if (success)
                            {
                                if (target.IsBusy())
                                {
                                    Character.SendChatMessage("The other player is busy at the moment.");
                                }
                                else
                                {
                                    var targetLastRequest = GetLastRequestOf(target);
                                    if (targetLastRequest == Character || targetLastRequest != null && targetLastRequest.Name.Equals(Character.Name))
                                    {
                                        LastRequest = null;
                                        SetLastRequestOf(target, null);
                                        target.Interrupt(this);
                                        StartDuelSession(target);
                                    }
                                    else
                                    {
                                        var characterDuelChoiceScreenScript = Character.ServiceProvider.GetRequiredService<DuelChoiceScreenScript>();
                                        characterDuelChoiceScreenScript.Target = target;
                                        characterDuelChoiceScreenScript.Callback = staking =>
                                        {
                                            SetStakingOf(target, staking);
                                            LastRequest = target;
                                            Character.SendChatMessage("Challenging " + target.DisplayName + "...");
                                            target.SendChatMessage("wishes to duel with you (" + (staking ? "stake" : "friendly") + ").",
                                                ChatMessageType.DuelRequestMessage,
                                                Character.DisplayName,
                                                Character.PreviousDisplayName);
                                        };
                                        Character.Widgets.OpenWidget(1369,
                                            0,
                                            characterDuelChoiceScreenScript,
                                            false);
                                    }
                                }
                            }
                            else
                            {
                                Character.SendChatMessage(GameStrings.YouCantReachThat);
                            }
                        });
                    Character.QueueTask(task);
                });
        }

        /// <summary>
        ///     Starts the duel session.
        /// </summary>
        /// <param name="target">The target.</param>
        private void StartDuelSession(ICharacter target)
        {
            Stage = DuelStage.First;
            DuelSession = true;
            SelfAccepted = false;
            TargetAccepted = false;
            _interfacesClosed = false;
            Target = target;

            var characterDuelScreenScript = Character.ServiceProvider.GetRequiredService<DuelScreenScript>();
            characterDuelScreenScript.Script = this;
            characterDuelScreenScript.CloseCallback = () =>
            {
                if (DuelSession)
                {
                    Target.SendChatMessage("The other player declined the duel.");
                }

                CancelDuelSession();
            };
            Character.Widgets.OpenWidget(1367, 0, characterDuelScreenScript, false);
            var targetDuelScreenScript = Target.ServiceProvider.GetRequiredService<DuelScreenScript>();
            targetDuelScreenScript.Script = this;
            targetDuelScreenScript.CloseCallback = () =>
            {
                if (DuelSession)
                {
                    Character.SendChatMessage("The other player declined the duel.");
                }

                CancelDuelSession();
            };
            Target.Widgets.OpenWidget(1367, 0, targetDuelScreenScript, false);

            SelfInterface = Character.Widgets.GetOpenWidget(1367);
            TargetInterface = Target.Widgets.GetOpenWidget(1367);
            if (SelfInterface == null || TargetInterface == null)
            {
                CancelDuelSession();
                return;
            }

            CurrentRules = new DuelRules(rule =>
            {
                if (SelfAccepted && !TargetAccepted)
                {
                    Character.Configurations.SendGlobalCs2Int(1332, 1); // warning for change
                    Character.QueueTask(new RsTask(() =>
                        {
                            Character.Configurations.SendGlobalCs2Int(1332, 0); // disable warning
                        },
                        5));
                }
                else if (TargetAccepted && !SelfAccepted)
                {
                    Target.Configurations.SendGlobalCs2Int(1332, 1); // warning for change
                    Target.QueueTask(new RsTask(() =>
                        {
                            Target.Configurations.SendGlobalCs2Int(1332, 0); // disable warning
                        },
                        5));
                }

                SelfAccepted = false;
                TargetAccepted = false;
                RefreshDuelConfirmationStatus();
            });

            Character.Configurations.SendStandardConfiguration(286, 0); // reset rules
            Target.Configurations.SendStandardConfiguration(286, 0); // reset rules
            Character.Configurations.SendGlobalCs2Int(545, IsStaking ? 1 : 0); // enables/disables staking tab for all screens
            Target.Configurations.SendGlobalCs2Int(545, IsStaking ? 1 : 0); // enables/disables staking tab for all screens
            Character.Configurations.SendGlobalCs2String(377, Target.DisplayName + " - " + Target.Statistics.FullCombatLevel);
            Target.Configurations.SendGlobalCs2String(377, Character.DisplayName + " - " + Character.Statistics.FullCombatLevel);
            Character.Configurations.SendGlobalCs2String(378, string.Empty); // reset confirmation status
            Target.Configurations.SendGlobalCs2String(378, string.Empty); // reset confirmation status

            SelfInterface.AttachClickHandler(42,
                (componentId, clickType, extra1, extra2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    CurrentRules.Hydrate(PreviousRules);
                    CurrentRules.SendRules(Character, Target);
                    return true;
                });
            TargetInterface.AttachClickHandler(42,
                (componentId, clickType, extra1, extra2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    var script = Target.GetOrAddScript<DuelArenaScript>();

                    CurrentRules.Hydrate(script.PreviousRules);
                    CurrentRules.SendRules(Character, Target);

                    return true;
                });

            SelfInterface.AttachClickHandler(48,
                (componentId, clickType, extra1, extra2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    CurrentRules.Hydrate(FavoritedRules);
                    CurrentRules.SendRules(Character, Target);
                    return true;
                });
            TargetInterface.AttachClickHandler(48,
                (componentId, clickType, extra1, extra2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    var script = Target.GetOrAddScript<DuelArenaScript>();

                    CurrentRules.Hydrate(script.FavoritedRules);
                    CurrentRules.SendRules(Character, Target);

                    return true;
                });

            SelfInterface.AttachClickHandler(56,
                (componentId, clickType, extra1, extra2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    if (SelfAccepted)
                    {
                        return true;
                    }

                    SelfAccepted = true;
                    RefreshDuelConfirmationStatus();
                    return true;
                });
            TargetInterface.AttachClickHandler(56,
                (componentId, clickType, extra1, extra2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    if (TargetAccepted)
                    {
                        return true;
                    }

                    TargetAccepted = true;
                    RefreshDuelConfirmationStatus();
                    return true;
                });

            SelfInterface.AttachClickHandler(71,
                (componentId, clickType, extra1, extra2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    Character.SendChatMessage("Your favorited dueling rules have been saved.");
                    FavoritedRules = CurrentRules.Dehydrate();
                    return true;
                });
            TargetInterface.AttachClickHandler(71,
                (componentId, clickType, extra1, extra2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    Target.SendChatMessage("Your favorited dueling rules have been saved.");
                    var script = Target.GetOrAddScript<DuelArenaScript>();
                    script.FavoritedRules = CurrentRules.Dehydrate();

                    return true;
                });

            SetupDuelStakeScreen();
            RefreshDuelConfirmationStatus();
            RefreshDuelStakeScreen();
        }

        /// <summary>
        ///     Setups the duel stake screen.
        /// </summary>
        public void SetupDuelStakeScreen()
        {
            if (!DuelSession || !IsStaking)
            {
                return;
            }

            // extend the screen to support staking // args: IY enable or IIY disable
            // this.Character.Configurations.SendCS2Script(6910, new object[] { ((1367 << 16) | 1), ((1367 << 16) | 1) });
            // this.Target.Configurations.SendCS2Script(6910, new object[] { ((1367 << 16) | 1), ((1367 << 16) | 1) });

            Character.Widgets.OpenInventoryOverlay(1368, 1, Character.ServiceProvider.GetRequiredService<DefaultWidgetScript>());
            Target.Widgets.OpenInventoryOverlay(1368, 1, Target.ServiceProvider.GetRequiredService<DefaultWidgetScript>());
            if (!Character.Widgets.OpenInventoryOverlay(1368, 1, Character.ServiceProvider.GetRequiredService<DefaultWidgetScript>()) ||
                !Target.Widgets.OpenInventoryOverlay(1368, 1, Target.ServiceProvider.GetRequiredService<DefaultWidgetScript>()))
            {
                CancelDuelSession();
                return;
            }

            SelfOverlay = Character.Widgets.GetOpenWidget(1368);
            TargetOverlay = Target.Widgets.GetOpenWidget(1368);
            if (SelfOverlay == null || TargetOverlay == null)
            {
                CancelDuelSession();
                return;
            }

            SelfContainer = [];
            TargetContainer = [];

            SelfOverlay.SetOptions(0,
                0,
                27,
                0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x40 | 0x80 | 0x400); // allow clicking of 7 right click options + auto examine option ( last )
            TargetOverlay.SetOptions(0,
                0,
                27,
                0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x40 | 0x80 | 0x400); // allow clicking of 7 right click options + auto examine option ( last )

            Character.Configurations.SendCs2Script(150, [(1368 << 16) | 0, 93, 4, 7, 1, -1, "Stake", "Stake-5", "Stake-10", "Stake-All", "Stake-X"]);
            Target.Configurations.SendCs2Script(150, [(1368 << 16) | 0, 93, 4, 7, 1, -1, "Stake", "Stake-5", "Stake-10", "Stake-All", "Stake-X"]);

            SelfInterface.SetOptions(7,
                0,
                27,
                0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x40 | 0x80 | 0x400); // allow clicking of 7 right click options + auto examine option ( last )
            SelfInterface.SetOptions(13, 0, 27, 0x2 | 0x400); // allow clicking of one option + auto examine option ( last )

            TargetInterface.SetOptions(7,
                0,
                27,
                0x2 | 0x4 | 0x8 | 0x10 | 0x20 | 0x40 | 0x80 | 0x400); // allow clicking of 7 right click options + auto examine option ( last )
            TargetInterface.SetOptions(13, 0, 27, 0x2 | 0x400); // allow clicking of one option + auto examine option ( last )

            Character.Configurations.SendCs2Script(150, [(1367 << 16) | 7, 134, 3, 3, 1, -1, "Remove", "Remove-5", "Remove-10", "Remove-All", "Remove-X"]);
            Character.Configurations.SendCs2Script(158, [(1367 << 16) | 13, 134, 3, 3, 1, -1, "Value", "", "", ""]);

            Target.Configurations.SendCs2Script(150, [(1367 << 16) | 7, 134, 3, 3, 1, -1, "Remove", "Remove-5", "Remove-10", "Remove-All", "Remove-X"]);
            Target.Configurations.SendCs2Script(158, [(1367 << 16) | 13, 134, 3, 3, 1, -1, "Value", "", "", ""]);

            SelfOverlay.AttachClickHandler(0,
                (componentID, clickType, itemID, itemSlot) =>
                {
                    if (itemSlot < 0 || itemSlot >= Character.Inventory.Capacity)
                    {
                        return false;
                    }

                    var item = Character.Inventory[itemSlot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    if (!item.ItemScript.CanTradeItem(item, Character))
                    {
                        Character.SendChatMessage("You can't trade this item.");
                        return false;
                    }

                    var count = 0;
                    var max = Character.Inventory.GetCount(item);
                    if (max <= 0)
                    {
                        return false;
                    }

                    if (clickType == ComponentClickType.LeftClick)
                    {
                        count = 1;
                    }
                    else if (clickType == ComponentClickType.Option2Click)
                    {
                        count = 5;
                    }
                    else if (clickType == ComponentClickType.Option3Click)
                    {
                        count = 10;
                    }
                    else if (clickType == ComponentClickType.Option4Click)
                    {
                        count = max;
                    }
                    else if (clickType == ComponentClickType.Option5Click)
                    {
                        OnIntInput handler = null;
                        handler = amt =>
                        {
                            Character.Widgets.IntInputHandler = null;
                            if (SelfIntInputHandler != handler)
                            {
                                return;
                            }

                            SelfIntInputHandler = null;
                            if (amt <= 0)
                            {
                                return;
                            }

                            var rem = item.Clone();
                            rem.Count = amt > max ? max : amt;
                            if (!SelfContainer.HasSpaceFor(rem))
                            {
                                Character.SendChatMessage("The stake is full.");
                                return;
                            }

                            var cnt = Character.Inventory.Remove(rem);
                            if (cnt <= 0)
                            {
                                return;
                            }

                            var add = item.Clone();
                            add.Count = cnt;
                            SelfContainer.Add(add);
                            RefreshDuelStakeScreen();
                            ProcessDuelStakeChange(true, false);
                        };
                        SelfIntInputHandler = Character.Widgets.IntInputHandler = handler;
                        Character.Configurations.SendIntegerInput("Please enter the amount to stake:");
                        return true;
                    }
                    else if (clickType == ComponentClickType.Option10Click) // examine
                    {
                        Character.SendChatMessage(item.ItemScript.GetExamine(item));
                    }

                    if (count > 0)
                    {
                        if (count > max)
                        {
                            count = max;
                        }

                        var toRemove = item.Clone();
                        toRemove.Count = count;
                        if (!SelfContainer.HasSpaceFor(toRemove))
                        {
                            Character.SendChatMessage("The stake is full.");
                            return false;
                        }

                        count = Character.Inventory.Remove(toRemove, itemSlot);
                        if (count <= 0)
                        {
                            return false;
                        }

                        var toAdd = item.Clone();
                        toAdd.Count = count;
                        SelfContainer.Add(toAdd);
                        RefreshDuelStakeScreen();
                        ProcessDuelStakeChange(true, false);
                    }

                    return true;
                });

            TargetOverlay.AttachClickHandler(0,
                (componentID, clickType, itemID, itemSlot) =>
                {
                    if (itemSlot < 0 || itemSlot >= Target.Inventory.Capacity)
                    {
                        return false;
                    }

                    var item = Target.Inventory[itemSlot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    if (!item.ItemScript.CanTradeItem(item, Target))
                    {
                        Target.SendChatMessage("You can't stake this item.");
                        return false;
                    }

                    var count = 0;
                    var max = Target.Inventory.GetCount(item);
                    if (max <= 0)
                    {
                        return false;
                    }

                    if (clickType == ComponentClickType.LeftClick)
                    {
                        count = 1;
                    }
                    else if (clickType == ComponentClickType.Option2Click)
                    {
                        count = 5;
                    }
                    else if (clickType == ComponentClickType.Option3Click)
                    {
                        count = 10;
                    }
                    else if (clickType == ComponentClickType.Option4Click)
                    {
                        count = max;
                    }
                    else if (clickType == ComponentClickType.Option5Click)
                    {
                        OnIntInput handler = null;
                        handler = amt =>
                        {
                            Target.Widgets.IntInputHandler = null;
                            if (TargetIntInputHandler != handler)
                            {
                                return;
                            }

                            TargetIntInputHandler = null;
                            if (amt <= 0)
                            {
                                return;
                            }

                            var rem = item.Clone();
                            rem.Count = amt > max ? max : amt;
                            if (!TargetContainer.HasSpaceFor(rem))
                            {
                                Target.SendChatMessage("The stake is full.");
                                return;
                            }

                            var cnt = Target.Inventory.Remove(rem);
                            if (cnt <= 0)
                            {
                                return;
                            }

                            var add = item.Clone();
                            add.Count = cnt;
                            TargetContainer.Add(add);
                            RefreshDuelStakeScreen();
                            ProcessDuelStakeChange(false, false);
                        };
                        TargetIntInputHandler = Target.Widgets.IntInputHandler = handler;
                        Target.Configurations.SendIntegerInput("Please enter the amount to stake:");
                        return true;
                    }
                    else if (clickType == ComponentClickType.Option10Click) // examine
                    {
                        Target.SendChatMessage(item.ItemScript.GetExamine(item));
                    }

                    if (count > 0)
                    {
                        if (count > max)
                        {
                            count = max;
                        }

                        var toRemove = item.Clone();
                        toRemove.Count = count;
                        if (!TargetContainer.HasSpaceFor(toRemove))
                        {
                            Target.SendChatMessage("The stake is full.");
                            return false;
                        }

                        count = Target.Inventory.Remove(toRemove, itemSlot);
                        if (count <= 0)
                        {
                            return false;
                        }

                        var toAdd = item.Clone();
                        toAdd.Count = count;
                        TargetContainer.Add(toAdd);
                        RefreshDuelStakeScreen();
                        ProcessDuelStakeChange(false, false);
                    }

                    return true;
                });

            // money pouch
            SelfInterface.AttachClickHandler(8,
                (componentId, clickType, extra1, extra2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    OnIntInput handler = null;
                    handler = amt =>
                    {
                        Character.Widgets.IntInputHandler = null;
                        if (SelfIntInputHandler != handler)
                        {
                            return;
                        }

                        SelfIntInputHandler = null;
                        if (amt <= 0)
                        {
                            return;
                        }

                        if (!SelfContainer.HasSpaceFor(new Item(995, amt)))
                        {
                            Character.SendChatMessage("The stake is full.");
                            return;
                        }

                        amt = Character.MoneyPouch.Remove(amt);
                        if (amt <= 0)
                        {
                            return;
                        }

                        SelfContainer.Add(new Item(995, amt));
                        RefreshDuelStakeScreen();
                        ProcessDuelStakeChange(true, false);
                    };
                    SelfIntInputHandler = Character.Widgets.IntInputHandler = handler;
                    Character.Configurations.SendIntegerInput("Please enter the amount to stake:");
                    return true;
                });

            TargetInterface.AttachClickHandler(8,
                (componentId, clickType, extra1, extra2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    OnIntInput handler = null;
                    handler = amt =>
                    {
                        Target.Widgets.IntInputHandler = null;
                        if (TargetIntInputHandler != handler)
                        {
                            return;
                        }

                        TargetIntInputHandler = null;
                        if (amt <= 0)
                        {
                            return;
                        }

                        if (!TargetContainer.HasSpaceFor(new Item(995, amt)))
                        {
                            Target.SendChatMessage("The stake is full.");
                            return;
                        }

                        amt = Target.MoneyPouch.Remove(amt);
                        if (amt <= 0)
                        {
                            return;
                        }

                        TargetContainer.Add(new Item(995, amt));
                        RefreshDuelStakeScreen();
                        ProcessDuelStakeChange(false, false);
                    };
                    TargetIntInputHandler = Target.Widgets.IntInputHandler = handler;
                    Target.Configurations.SendIntegerInput("Please enter the amount to stake:");
                    return true;
                });

            SelfInterface.AttachClickHandler(7,
                (componentID, clickType, itemID, itemSlot) =>
                {
                    if (itemSlot < 0 || itemSlot >= SelfContainer.Capacity)
                    {
                        return false;
                    }

                    var item = SelfContainer[itemSlot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    var count = 0;
                    var max = SelfContainer.GetCount(item);
                    if (max <= 0)
                    {
                        return false;
                    }

                    if (clickType == ComponentClickType.LeftClick)
                    {
                        count = 1;
                    }
                    else if (clickType == ComponentClickType.Option2Click)
                    {
                        count = 5;
                    }
                    else if (clickType == ComponentClickType.Option3Click)
                    {
                        count = 10;
                    }
                    else if (clickType == ComponentClickType.Option4Click)
                    {
                        count = max;
                    }
                    else if (clickType == ComponentClickType.Option5Click)
                    {
                        OnIntInput handler = null;
                        handler = amt =>
                        {
                            Character.Widgets.IntInputHandler = null;
                            if (SelfIntInputHandler != handler)
                            {
                                return;
                            }

                            SelfIntInputHandler = null;
                            if (amt <= 0)
                            {
                                return;
                            }

                            var rem = item.Clone();
                            rem.Count = amt > max ? max : amt;
                            var cnt = SelfContainer.Remove(rem, itemSlot);
                            if (cnt <= 0)
                            {
                                return;
                            }

                            var add = item.Clone();
                            add.Count = cnt;
                            if (add.Id == 995)
                            {
                                Character.MoneyPouch.Add(cnt);
                            }
                            else
                            {
                                Character.Inventory.Add(add);
                            }

                            RefreshDuelStakeScreen();
                            ProcessDuelStakeChange(true, true);
                        };
                        SelfIntInputHandler = Character.Widgets.IntInputHandler = handler;
                        Character.Configurations.SendIntegerInput("Please enter the amount to remove:");
                        return true;
                    }
                    else if (clickType == ComponentClickType.Option10Click)
                    {
                        Character.SendChatMessage(item.ItemScript.GetExamine(item));
                    }

                    if (count > 0)
                    {
                        if (count > max)
                        {
                            count = max;
                        }

                        var toRemove = item.Clone();
                        toRemove.Count = count;
                        count = SelfContainer.Remove(toRemove, itemSlot);
                        if (count <= 0)
                        {
                            return false;
                        }

                        var toAdd = item.Clone();
                        toAdd.Count = count;
                        if (toAdd.Id == 995)
                        {
                            Character.MoneyPouch.Add(count);
                        }
                        else
                        {
                            Character.Inventory.Add(toAdd);
                        }

                        RefreshDuelStakeScreen();
                        ProcessDuelStakeChange(true, true);
                    }

                    return true;
                });

            TargetInterface.AttachClickHandler(7,
                (componentID, clickType, itemID, itemSlot) =>
                {
                    if (itemSlot < 0 || itemSlot >= TargetContainer.Capacity)
                    {
                        return false;
                    }

                    var item = TargetContainer[itemSlot];
                    if (item == null || item.Id != itemID)
                    {
                        return false;
                    }

                    var count = 0;
                    var max = TargetContainer.GetCount(item);
                    if (max <= 0)
                    {
                        return false;
                    }

                    if (clickType == ComponentClickType.LeftClick)
                    {
                        count = 1;
                    }
                    else if (clickType == ComponentClickType.Option2Click)
                    {
                        count = 5;
                    }
                    else if (clickType == ComponentClickType.Option3Click)
                    {
                        count = 10;
                    }
                    else if (clickType == ComponentClickType.Option4Click)
                    {
                        count = max;
                    }
                    else if (clickType == ComponentClickType.Option5Click)
                    {
                        OnIntInput handler = null;
                        handler = amt =>
                        {
                            Target.Widgets.IntInputHandler = null;
                            if (TargetIntInputHandler != handler)
                            {
                                return;
                            }

                            TargetIntInputHandler = null;
                            if (amt <= 0)
                            {
                                return;
                            }

                            var rem = item.Clone();
                            rem.Count = amt > max ? max : amt;
                            var cnt = TargetContainer.Remove(rem, itemSlot);
                            if (cnt <= 0)
                            {
                                return;
                            }

                            var add = item.Clone();
                            add.Count = cnt;
                            if (add.Id == 995)
                            {
                                Target.MoneyPouch.Add(cnt);
                            }
                            else
                            {
                                Target.Inventory.Add(add);
                            }

                            RefreshDuelStakeScreen();
                            ProcessDuelStakeChange(false, true);
                        };
                        TargetIntInputHandler = Target.Widgets.IntInputHandler = handler;
                        Target.Configurations.SendIntegerInput("Please enter the amount to remove:");
                        return true;
                    }
                    else if (clickType == ComponentClickType.Option10Click)
                    {
                        Target.SendChatMessage(item.ItemScript.GetExamine(item));
                    }

                    if (count > 0)
                    {
                        if (count > max)
                        {
                            count = max;
                        }

                        var toRemove = item.Clone();
                        toRemove.Count = count;
                        count = TargetContainer.Remove(toRemove, itemSlot);
                        if (count <= 0)
                        {
                            return false;
                        }

                        var toAdd = item.Clone();
                        toAdd.Count = count;
                        if (toAdd.Id == 995)
                        {
                            Target.MoneyPouch.Add(count);
                        }
                        else
                        {
                            Target.Inventory.Add(toAdd);
                        }

                        RefreshDuelStakeScreen();
                        ProcessDuelStakeChange(false, true);
                    }

                    return true;
                });
        }

        /// <summary>
        ///     Refreshe's duel stake screen ( Items )
        /// </summary>
        public void RefreshDuelStakeScreen()
        {
            if (!DuelSession || !IsStaking)
            {
                return;
            }

            var selfFullUpdate = SelfContainer.Updates.Count == 0;
            var targetFullUpdate = TargetContainer.Updates.Count == 0;

            Character.Configurations.SendItems(134, false, SelfContainer, selfFullUpdate ? null : SelfContainer.Updates);
            Character.Configurations.SendItems(134, true, TargetContainer, targetFullUpdate ? null : TargetContainer.Updates);
            Target.Configurations.SendItems(134, false, TargetContainer, targetFullUpdate ? null : TargetContainer.Updates);
            Target.Configurations.SendItems(134, true, SelfContainer, selfFullUpdate ? null : SelfContainer.Updates);

            var selfTotal = SelfContainer.CalculateTotalValue();
            var targetTotal = TargetContainer.CalculateTotalValue();

            Character.Configurations.SendGlobalCs2Int(546, selfTotal);
            Character.Configurations.SendGlobalCs2Int(1333, targetTotal);

            Target.Configurations.SendGlobalCs2Int(546, targetTotal);
            Target.Configurations.SendGlobalCs2Int(1333, selfTotal);
        }

        /// <summary>
        ///     Refreshe's trade confirmation status.
        /// </summary>
        public void RefreshDuelConfirmationStatus()
        {
            if (!DuelSession)
            {
                return;
            }

            if (!SelfAccepted && !TargetAccepted)
            {
                Character.Configurations.SendBitConfiguration(12247, 0); // turn off Waiting for opponent
                Character.Configurations.SendGlobalCs2Int(9, 0); // turn off Waiting for opponent
                Target.Configurations.SendBitConfiguration(12247, 0); // turn off Waiting for opponent
                Target.Configurations.SendGlobalCs2Int(9, 0); // turn off Waiting for opponent
            }
            else if (SelfAccepted && !TargetAccepted)
            {
                Character.Configurations.SendBitConfiguration(12247, 1); // waiting for opponent
                Target.Configurations.SendGlobalCs2Int(9, 1); // opponent has accepted
            }
            else if (!SelfAccepted && TargetAccepted)
            {
                Character.Configurations.SendGlobalCs2Int(9, 1); // opponent has accepted
                Target.Configurations.SendBitConfiguration(12247, 1); // waiting for opponent
            }
            else // GOTO next step
            {
                if (Stage == DuelStage.First)
                {
                    StartConfirmationStage();
                }
                else if (Stage == DuelStage.Second)
                {
                    StartDuelCombatStage();
                }
            }
        }

        /// <summary>
        ///     Processes the duel change.
        /// </summary>
        /// <param name="self">if set to <c>true</c> [self].</param>
        public static void ProcessDuelChange(bool self) { }

        /// <summary>
        ///     Process'es duel stake change.
        /// </summary>
        public void ProcessDuelStakeChange(bool self, bool valueDecreased)
        {
            if (!DuelSession || !IsStaking)
            {
                return;
            }

            var selfAccepted = SelfAccepted;
            var targetAccepted = TargetAccepted;
            var accepted = selfAccepted | targetAccepted;
            SelfAccepted = false;
            TargetAccepted = false;
            if (accepted)
            {
                RefreshDuelConfirmationStatus();
            }

            var slots = self ? SelfContainer.Updates : TargetContainer.Updates;

            if (valueDecreased)
            {
                //if (self && targetAccepted)
                //this.TargetInterface.DrawString(37, "<col=FF0000><b>CHECK OTHER PLAYER'S OFFER!</b></col>");
                //else if (!self && selfAccepted)
                //this.SelfInterface.DrawString(37, "<col=FF0000><b>CHECK OTHER PLAYER'S OFFER!</b></col>");

                //foreach (short slot in slots)
                // {
                //this.Character.Configurations.SendCS2Script(143, new object[] { 335 << 16 | (self ? 31 : 33), 4, 7, (int)slot });
                //this.Target.Configurations.SendCS2Script(143, new object[] { 335 << 16 | (self ? 33 : 31), 4, 7, (int)slot });
                //}

                if (!SelfModified && self) { }
                else if (!TargetModified && !self) { }

                if (self)
                {
                    SelfModified = true;
                }
                else
                {
                    TargetModified = true;
                }
            }

            slots.Clear();
        }

        /// <summary>
        ///     Start's duel confirmation stage.
        /// </summary>
        public void StartConfirmationStage()
        {
            if (!DuelSession || Stage != DuelStage.First)
            {
                return;
            }

            SelfAccepted = false;
            TargetAccepted = false;

            if (!CurrentRules.CheckRules(Character, Target) || !CurrentRules.CheckRules(Target, Character))
            {
                RefreshDuelConfirmationStatus();
                return;
            }

            if (IsStaking)
            {
                var container = new GenericContainer(StorageType.Normal, (short)(SelfContainer.Capacity + TargetContainer.Capacity));
                container.AddRange(SelfContainer);
                container.AddRange(TargetContainer);

                var all = container.ToArray();

                if (!Character.Inventory.HasSpaceForRange(all))
                {
                    Character.SendChatMessage("You don't have enough space in your inventory for this duel.");
                    Target.SendChatMessage("Your opponent doesn't have enough space in their inventory for this duel.");
                    RefreshDuelConfirmationStatus();
                    return;
                }

                if (!Target.Inventory.HasSpaceForRange(all))
                {
                    Character.SendChatMessage("Your opponent doesn't have enough space in their inventory for this duel.");
                    Target.SendChatMessage("You don't have enough space in your inventory for this duel.");
                    RefreshDuelConfirmationStatus();
                    return;
                }
            }

            ((DuelScreenScript)SelfInterface.Script).CloseCallback = null;
            ((DuelScreenScript)TargetInterface.Script).CloseCallback = null;
            Character.Widgets.CloseWidget(SelfInterface);
            Target.Widgets.CloseWidget(TargetInterface);
            Character.Widgets.CloseWidget(SelfOverlay);
            Target.Widgets.CloseWidget(TargetOverlay);
            SelfIntInputHandler = null;
            TargetIntInputHandler = null;

            Stage = DuelStage.Second;

            var characterDuelConfirmScreen = Character.ServiceProvider.GetRequiredService<DuelConfirmScreenScript>();
            characterDuelConfirmScreen.CloseCallback = () =>
            {
                if (DuelSession)
                {
                    Target.SendChatMessage("Your opponent declined the duel.");
                }

                CancelDuelSession();
            };
            Character.Widgets.OpenWidget(1366,
                0,
                characterDuelConfirmScreen,
                false);

            var targetDuelConfirmScreen = Target.ServiceProvider.GetRequiredService<DuelConfirmScreenScript>();
            targetDuelConfirmScreen.CloseCallback = () =>
            {
                if (DuelSession)
                {
                    Character.SendChatMessage("Your opponent declined the duel.");
                }

                CancelDuelSession();
            };
            Target.Widgets.OpenWidget(1366,
                0,
                targetDuelConfirmScreen,
                false);

            var self = Character.Widgets.GetOpenWidget(1366);
            var target = Target.Widgets.GetOpenWidget(1366);
            if (self == null || target == null)
            {
                CancelDuelSession();
                return;
            }

            SelfInterface = self;
            TargetInterface = target;

            /*if (this.IsStaking)
            {
                if (this.SelfContainer.TakenSlots > 0)
                {
                    this.SelfInterface.DrawString(25, string.Empty);
                    this.TargetInterface.DrawString(26, string.Empty);
                }
                if (this.TargetContainer.TakenSlots > 0)
                {
                    this.SelfInterface.DrawString(26, string.Empty);
                    this.TargetInterface.DrawString(25, string.Empty);
                }
            }*/

            //if (this.SelfModified)
            //this.TargetInterface.SetVisible(55, true);
            //if (this.TargetModified)
            //this.SelfInterface.SetVisible(55, true);

            Character.Configurations.SendGlobalCs2String(377, Target.DisplayName); // name, no combat
            Target.Configurations.SendGlobalCs2String(377, Character.DisplayName); // name, no combat

            SelfInterface.AttachClickHandler(24,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    if (SelfAccepted)
                    {
                        return true;
                    }

                    SelfAccepted = true;
                    RefreshDuelConfirmationStatus();
                    return true;
                });

            TargetInterface.AttachClickHandler(24,
                (componentID, clickType, extraData1, extraData2) =>
                {
                    if (clickType != ComponentClickType.LeftClick)
                    {
                        return false;
                    }

                    if (TargetAccepted)
                    {
                        return true;
                    }

                    TargetAccepted = true;
                    RefreshDuelConfirmationStatus();
                    return true;
                });

            RefreshDuelConfirmationStatus();
        }

        /// <summary>
        ///     Starts the duel combat stage.
        /// </summary>
        private void StartDuelCombatStage()
        {
            if (!DuelSession || Stage != DuelStage.Second)
            {
                return;
            }

            ((DuelConfirmScreenScript)SelfInterface.Script).CloseCallback = null;
            ((DuelConfirmScreenScript)TargetInterface.Script).CloseCallback = null;
            CloseInterfaces();

            var regionService = Character.ServiceProvider.GetRequiredService<IMapRegionService>();
            ILocation selfLocation;
            ILocation targetLocation;
            var arenaId = RandomStatic.Generator.Next(3);
            if (CurrentRules[DuelRules.Rule.Obstacles])
            {
                while (true)
                {
                    selfLocation = DuelData.GetObstacleArena(arenaId);
                    if (!regionService.IsAccessible(selfLocation))
                    {
                        continue;
                    }

                    targetLocation = DuelData.GetObstacleArena(arenaId);
                    if (!regionService.IsAccessible(targetLocation))
                    {
                        continue;
                    }

                    if (!selfLocation.Equals(targetLocation))
                    {
                        break;
                    }
                }
            }
            else
            {
                while (true)
                {
                    selfLocation = DuelData.GetNormalArena(arenaId);
                    if (!regionService.IsAccessible(selfLocation))
                    {
                        continue;
                    }

                    targetLocation = DuelData.GetNormalArena(arenaId);
                    if (!regionService.IsAccessible(targetLocation))
                    {
                        continue;
                    }

                    if (!selfLocation.Equals(targetLocation))
                    {
                        break;
                    }
                }
            }

            if (CurrentRules[DuelRules.Rule.NoMovement])
            {
                var found = false;
                for (var x = 0; !found; x++)
                {
                    for (var y = 0; !found; y++)
                    {
                        targetLocation = selfLocation.Translate(x, y, 0);
                        if (regionService.IsAccessible(targetLocation) && !selfLocation.Equals(targetLocation))
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }

            Character.Movement.Teleport(Teleport.Create(selfLocation));
            Target.Movement.Teleport(Teleport.Create(targetLocation));

            CurrentRules.RemoveEquipment(Character);
            CurrentRules.RemoveEquipment(Target);

            Character.Statistics.NormalizeBoostedStatistics();
            Target.Statistics.NormalizeBoostedStatistics();

            Character.Prayers.DeactivateAllPrayers();
            Target.Prayers.DeactivateAllPrayers();

            Character.AddScript(new DuelArenaCombatScript(Character.ServiceProvider.GetRequiredService<ICharacterContextAccessor>(),
                Target,
                CurrentRules,
                SelfContainer,
                TargetContainer,
                _hintIconBuilder));
            Target.AddScript(new DuelArenaCombatScript(Target.ServiceProvider.GetRequiredService<ICharacterContextAccessor>(),
                Character,
                CurrentRules,
                TargetContainer,
                SelfContainer,
                _hintIconBuilder));

            var previousRules = CurrentRules.Dehydrate();
            PreviousRules = previousRules;
            var script = Target.GetScript<DuelArenaScript>();
            if (script != null)
            {
                script.PreviousRules = previousRules;
            }

            Character.Profile.SetObject(DuelArenaConstants.MinigamesDuelArenaFavoriteRules, FavoritedRules);
            Character.Profile.SetObject(DuelArenaConstants.MinigamesDuelArenaPreviousRules, PreviousRules);

            Character.TryRemoveScript<DuelArenaScript>();
            Target.TryRemoveScript<DuelArenaScript>();
        }

        /// <summary>
        ///     Cancels the duel session.
        /// </summary>
        private void CancelDuelSession()
        {
            if (!DuelSession)
            {
                return;
            }

            DuelSession = false;
            CloseInterfaces();
            if (SelfContainer != null && SelfContainer.TakenSlots > 0)
            {
                for (var i = 0; i < SelfContainer.Capacity; i++)
                {
                    if (SelfContainer[i] == null)
                    {
                        continue;
                    }

                    if (SelfContainer[i].Id == 995)
                    {
                        Character.MoneyPouch.Add(SelfContainer[i].Count);
                    }
                    else
                    {
                        Character.Inventory.Add(SelfContainer[i]);
                    }
                }
            }

            if (Target != null && TargetContainer != null && TargetContainer.TakenSlots > 0)
            {
                for (var i = 0; i < TargetContainer.Capacity; i++)
                {
                    if (TargetContainer[i] == null)
                    {
                        continue;
                    }

                    if (TargetContainer[i].Id == 995)
                    {
                        Target.MoneyPouch.Add(TargetContainer[i].Count);
                    }
                    else
                    {
                        Target.Inventory.Add(TargetContainer[i]);
                    }
                }
            }

            Stage = DuelStage.Request;
            IsStaking = false;
            Target = null;
            SelfContainer = null;
            TargetContainer = null;
        }

        /// <summary>
        ///     Closes the interfaces.
        /// </summary>
        private void CloseInterfaces()
        {
            if (Character.Widgets.IntInputHandler == SelfIntInputHandler)
            {
                Character.Widgets.IntInputHandler = null;
            }

            if (Target.Widgets.IntInputHandler == TargetIntInputHandler)
            {
                Target.Widgets.IntInputHandler = null;
            }

            if (SelfInterface != null && SelfInterface.IsOpened)
            {
                Character.Widgets.CloseWidget(SelfInterface);
            }

            if (TargetInterface != null && TargetInterface.IsOpened)
            {
                Target.Widgets.CloseWidget(TargetInterface);
            }

            if (SelfOverlay != null && SelfOverlay.IsOpened)
            {
                Character.Widgets.CloseWidget(SelfOverlay);
            }

            if (TargetOverlay != null && TargetOverlay.IsOpened)
            {
                Target.Widgets.CloseWidget(TargetOverlay);
            }

            SelfInterface = null;
            TargetInterface = null;
            SelfOverlay = null;
            TargetOverlay = null;
            SelfIntInputHandler = null;
            TargetIntInputHandler = null;
            SelfAccepted = false;
            TargetAccepted = false;
            _interfacesClosed = true;
        }

        /// <summary>
        ///     Get's called when character is interrupted.
        ///     By default this method does nothing.
        /// </summary>
        /// <param name="source">
        ///     Object which performed the interruption,
        ///     this parameter can be null , but it is not encouraged to do so.
        ///     Best use would be to set the invoker class instance as source.
        /// </param>
        public override void OnInterrupt(object source)
        {
            if (source == this)
            {
                return;
            }

            base.OnInterrupt(source);
            CancelDuelSession();
        }

        /// <summary>
        ///     Tick's character.
        ///     By default, this method does nothing.
        /// </summary>
        public override void Tick()
        {
            if (DuelSession)
            {
                if (Target == null || Target.IsDestroyed)
                {
                    CancelDuelSession();
                }

                if (Character == null || Character.IsDestroyed)
                {
                    CancelDuelSession();
                }

                if (Stage == DuelStage.First || Stage == DuelStage.Second)
                {
                    if (!_interfacesClosed && !SelfInterface.IsOpened)
                    {
                        CancelDuelSession();
                    }

                    if (!_interfacesClosed && !TargetInterface.IsOpened)
                    {
                        CancelDuelSession();
                    }

                    switch (Stage)
                    {
                        // duel step 1
                        case DuelStage.First:
                            {
                                if (IsStaking && !_interfacesClosed && (!SelfOverlay.IsOpened || !TargetOverlay.IsOpened))
                                {
                                    CancelDuelSession();
                                }

                                //if (this.Character.Inventory.FreeSlots != this.LastMyInventoryFreeSlots || this.Target.Inventory.FreeSlots != this.LastTargetInventoryFreeSlots)
                                //this.RefreshFreeInventorySlots();
                                break;
                            }
                        case DuelStage.Second:
                            {
                                if (IsStaking && !_interfacesClosed && (SelfOverlay.IsOpened || TargetOverlay.IsOpened))
                                {
                                    CancelDuelSession();
                                }

                                break;
                            }
                    }
                }
            }

            if (LastRequest == null)
            {
                return;
            }

            if (LastRequest.IsDestroyed || !Character.Viewport.InBounds(LastRequest.Location))
            {
                LastRequest = null;
            }
        }

        /// <summary>
        ///     Get's last request of the other character.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        private static ICharacter? GetLastRequestOf(ICharacter other)
        {
            var duel = other.GetScript<DuelArenaScript>();
            return duel?.LastRequest;
        }

        /// <summary>
        ///     Set's last request of the other character.
        /// </summary>
        /// <param name="other"></param>
        /// <param name="request"></param>
        private static void SetLastRequestOf(ICharacter other, ICharacter? request)
        {
            var duel = other.GetScript<DuelArenaScript>();
            if (duel != null)
            {
                duel.LastRequest = request;
            }
        }

        /// <summary>
        ///     Sets the staking of.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <param name="staking">if set to <c>true</c> [staking].</param>
        private static void SetStakingOf(ICharacter other, bool staking)
        {
            var duel = other.GetScript<DuelArenaScript>();
            if (duel != null)
            {
                duel.IsStaking = staking;
            }
        }
    }

    /// <summary>
    ///     Container for holding items in trade offer interfaces.
    /// </summary>
    public class DuelContainer : BaseItemContainer
    {
        /// <summary>
        ///     Contains last slots update.
        /// </summary>
        public HashSet<int> Updates { get; }

        /// <summary>
        ///     Construct's new trade container.
        /// </summary>
        public DuelContainer()
            : base(StorageType.Normal, 9) =>
            Updates = [];

        /// <summary>
        ///     Happens when trade container get's updated.
        /// </summary>
        /// <param name="slots"></param>
        public override void OnUpdate(HashSet<int>? slots = null)
        {
            if (slots == null)
            {
                Updates.Clear();
            }
            else
            {
                Updates.AddRange(slots);
            }
        }

        /// <summary>
        ///     Calculate's total value of this container.
        /// </summary>
        /// <returns></returns>
        public int CalculateTotalValue()
        {
            var total = 0;
            for (var slot = 0; slot < Capacity; slot++)
            {
                var item = this[slot];
                if (item == null)
                {
                    continue;
                }

                if ((ulong)total + (ulong)item.ItemDefinition.TradeValue * (ulong)item.Count > int.MaxValue)
                {
                    return -1;
                }

                total += item.ItemDefinition.TradeValue * item.Count;
            }

            return total;
        }
    }
}