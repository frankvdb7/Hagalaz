using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Resources;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.Clans;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    ///     Represents the clan chat tab.
    /// </summary>
    public class ClanChatTab : WidgetScript
    {
        /// <summary>
        ///     Contains the prefex input.
        /// </summary>
        private OnStringInput? _prefexInput;

        public ClanChatTab(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            // clan info screen
            InterfaceInstance.AttachClickHandler(75, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    if (!Owner.HasClan())
                    {
                        Owner.SendChatMessage("You must be in a clan to do that.");
                        return false;
                    }

                    var clanDetails = Owner.ServiceProvider.GetRequiredService<ClanDetails>();
                    Owner.Widgets.OpenWidget(1107, 0, clanDetails, true);
                    return true;
                }

                return false;
            });

            // clan setup screen
            InterfaceInstance.AttachClickHandler(78, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    if (!Owner.HasClan())
                    {
                        Owner.SendChatMessage("You must be in a clan to do that.");
                        return false;
                    }

                    var clanSetup = Owner.ServiceProvider.GetRequiredService<ClanSetup>();
                    Owner.Widgets.OpenWidget(1096, 0, clanSetup, true);
                    return true;
                }

                return false;
            });

            // leave/join clan channel
            InterfaceInstance.AttachClickHandler(82, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    if (!Owner.HasClan())
                    {
                        Owner.SendChatMessage("You must be in a clan to do that.");
                        return true;
                    }

                    //if (Owner.ChatChannels.InCcChannel)
                    //{
                    //    Owner.ChatChannels.LeaveCcChannel();
                    //}
                    //else
                    //{
                    //    Owner.ChatChannels.JoinCcChannel(Owner.Clan.Name);
                    //}

                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachUseOnCreatureHandler(87, (componentID, usedOn, forceRun, extraData1, extraData2) =>
            {
                if (usedOn is ICharacter target)
                {
                    Owner.Interrupt(this);
                    Owner.ForceRunMovementType(forceRun);
                    Owner.QueueTask(new CreatureReachTask(Owner, usedOn, success =>
                    {
                        Owner.Interrupt(this);
                        if (success)
                        {
                            if (target.IsBusy())
                            {
                                Owner.SendChatMessage("The other player is busy at the moment.");
                            }
                            else
                            {
                                Owner.SendChatMessage("Sending clan invite...");
                                target.SendChatMessage("is asking you to help found a clan.", ChatMessageType.ClanRequest, Owner.DisplayName);
                            }
                        }
                        else
                        {
                            Owner.SendChatMessage(GameStrings.YouCantReachThat);
                        }
                    }));
                    //character.Interfaces.OpenStandartInterface(1095, 0, new Invite(this.owner), true);
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(91, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    //if (Owner.ChatChannels.InGuestCcChannel)
                    //{
                    //    Owner.ChatChannels.LeaveGuestCcChannelAsync();
                    //}
                    //else
                    //{
                    //    _prefexInput = Owner.Interfaces.StringInputHandler = clanName =>
                    //    {
                    //        _prefexInput = Owner.Interfaces.StringInputHandler = null;
                    //        if (Owner.HasClan() && Owner.Clan.Name.Equals(clanName, StringComparison.OrdinalIgnoreCase))
                    //        {
                    //            Owner.SendChatMessage("You cannot join the guest clan chat channel of your own clan!");
                    //            return;
                    //        }

                    //        if (!StringUtilities.IsValidName(clanName))
                    //        {
                    //            Owner.SendChatMessage("Please enter a valid clan name.");
                    //            return;
                    //        }

                    //        Owner.ChatChannels.JoinGuestCcChannel(clanName);
                    //    };
                    //    Owner.Configurations.SendStringInput("Please enter the name of the clan to chat in:");
                    //}

                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(109, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    if (!Owner.HasClan())
                    {
                        Owner.SendChatMessage("You're currently not in a clan.");
                        return false;
                    }

                    //Owner.Session.SendPacketAsync(new RemoveClanMemberRequestPacketComposer(Owner.MasterId, Owner.Clan.Name));
                    return true;
                }

                return false;
            });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (_prefexInput != null)
            {
                if (Owner.Widgets.StringInputHandler == _prefexInput)
                {
                    Owner.Widgets.StringInputHandler = null;
                }

                _prefexInput = null;
            }
        }
    }
}