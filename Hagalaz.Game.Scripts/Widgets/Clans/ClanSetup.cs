using Hagalaz.Game.Abstractions.Builders.Widget;
using Hagalaz.Game.Abstractions.Features.Clans;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Features.Clans;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Clans
{
    /// <summary>
    /// </summary>
    public class ClanSetup : WidgetScript
    {
        private readonly IWidgetOptionBuilder _widgetOptionBuilder;

        /// <summary>
        ///     The member details
        /// </summary>
        private IClanMember _memberDetails;

        /// <summary>
        ///     The member's job
        /// </summary>
        private ClanJob _memberJob;

        /// <summary>
        ///     The member rank
        /// </summary>
        private ClanRank _memberRank;

        public ClanSetup(ICharacterContextAccessor characterContextAccessor, IWidgetOptionBuilder widgetOptionBuilder) : base(characterContextAccessor) => _widgetOptionBuilder = widgetOptionBuilder;

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
            ShowClanMatesTab();

            var options = _widgetOptionBuilder.SetRightClickOption(0, true).Value;

            // clanmates
            InterfaceInstance.SetOptions(45, 0, Clan.MaxMembers, options); // member details
            InterfaceInstance.SetOptions(280, 0, (ushort)sbyte.MaxValue, options); // member rank
            InterfaceInstance.SetOptions(266, 0, byte.MaxValue, options); // member job

            // clansettings
            //this.interfaceInstance.SetOptions(211, 0, 100, options); // guest access citadel
            InterfaceInstance.SetVisible(192, false); // guest access citadel
            InterfaceInstance.SetVisible(197, false); // signpost permissions
            InterfaceInstance.SetVisible(199, false); // guest access citadel
            InterfaceInstance.SetVisible(214, false); // signpost permissions
            InterfaceInstance.SetOptions(244, 0, 144, options); // timezone
            InterfaceInstance.SetOptions(294, 0, 200, options); // world id

            SelectPermissionCategory(1);

            InterfaceInstance.AttachClickHandler(45, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    var member = Owner.Clan.GetMemberByIndex(extraData2);
                    if (member != null)
                    {
                        SetMemberDetails(member);
                    }

                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(98, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Clan.Settings.RankToEnterCc = Owner.Clan.Settings.RankToEnterCc == ClanRank.Guest ? ClanRank.Recruit : ClanRank.Guest;
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(99, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Clan.Settings.RankToTalk = Owner.Clan.Settings.RankToTalk == ClanRank.Guest ? ClanRank.Recruit : ClanRank.Guest;
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(100, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Clan.Settings.Recruiting = !Owner.Clan.Settings.Recruiting;
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(101, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Clan.Settings.ClanTime = !Owner.Clan.Settings.ClanTime;
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(117, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    ShowClanMatesTab();
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(124, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    ShowClanSettingsTab();
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(128, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    var script = Owner.ServiceProvider.GetRequiredService<MottifScript>();
                    Owner.Widgets.OpenWidget(1105, 0, script, false); // mottif designer
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(164, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    var script = Owner.ServiceProvider.GetRequiredService<DefaultWidgetScript>();
                    Owner.Widgets.OpenWidget(1097, 0, script, false); // keywords
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(244, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Clan.Settings.TimeZone = extraData2;
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(266, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _memberJob = (ClanJob)extraData2;
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(280, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _memberRank = (ClanRank)extraData2;
                    return true;
                }

                return false;
            });


            InterfaceInstance.AttachClickHandler(294, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    if (extraData2 < 0 || extraData2 > 200)
                    {
                        return false;
                    }

                    Owner.Clan.Settings.WorldID = (ushort)extraData2;
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(313, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    // TODO - Kick member out clan
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(322, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    SaveMemberDetails();
                    return true;
                }

                return false;
            });

            InterfaceInstance.AttachClickHandler(350, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    //Owner.Session.SendPacketAsync(new ClanPacketComposer(Owner.Clan, false));
                    var script = Owner.ServiceProvider.GetRequiredService<NationalFlag>();
                    Owner.Widgets.OpenWidget(1089, 0, script, false); // international flag
                    return true;
                }

                return false;
            });


            InterfaceInstance.AttachClickHandler(389, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    ShowClanPermissionsTab();
                    return true;
                }

                return false;
            });

            OnComponentClick selectPermissionRankClick = (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    SelectPermisionRank((componentID - 398) / 8);
                    return true;
                }

                return false;
            };
            for (short i = 398; i <= 478; i += 8)
            {
                InterfaceInstance.AttachClickHandler(i, selectPermissionRankClick);
            }

            OnComponentClick selectPermissionTabClick = (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    if (componentID == 492)
                    {
                        SelectPermissionCategory(1);
                    }
                    else
                    {
                        SelectPermissionCategory((componentID - 501) / 8 + 2);
                    }

                    return true;
                }

                return false;
            };
            InterfaceInstance.AttachClickHandler(492, selectPermissionTabClick);
            for (short i = 501; i <= 525; i += 8)
            {
                InterfaceInstance.AttachClickHandler(i, selectPermissionTabClick);
            }
        }

        /// <summary>
        ///     Switches to clan mates tab.
        /// </summary>
        public void ShowClanMatesTab()
        {
            ToggleClanMatesTab(true);
            ToggleClanSettingsTab(false);
            ToggleClanPermissionsTab(false);
        }

        /// <summary>
        ///     Shows the clan settings tab.
        /// </summary>
        public void ShowClanSettingsTab()
        {
            ToggleClanMatesTab(false);
            ToggleClanSettingsTab(true);
            ToggleClanPermissionsTab(false);
        }

        /// <summary>
        ///     Shows the clan permissions tab.
        /// </summary>
        public void ShowClanPermissionsTab()
        {
            ToggleClanMatesTab(false);
            ToggleClanSettingsTab(false);
            ToggleClanPermissionsTab(true);
        }

        /// <summary>
        ///     Toggles the clan mates tab.
        /// </summary>
        /// <param name="toggle">if set to <c>true</c> [toggle].</param>
        private void ToggleClanMatesTab(bool toggle)
        {
            InterfaceInstance.SetVisible(87, toggle);
            InterfaceInstance.SetVisible(114, toggle);
        }

        /// <summary>
        ///     Toggles the clan settings tab.
        /// </summary>
        /// <param name="toggle">if set to <c>true</c> [toggle].</param>
        private void ToggleClanSettingsTab(bool toggle)
        {
            InterfaceInstance.SetVisible(88, toggle);
            InterfaceInstance.SetVisible(121, toggle);
        }

        /// <summary>
        ///     Toggles the clan permissions tab.
        /// </summary>
        /// <param name="toggle">if set to <c>true</c> [toggle].</param>
        private void ToggleClanPermissionsTab(bool toggle)
        {
            InterfaceInstance.SetVisible(89, toggle);
            InterfaceInstance.SetVisible(388, toggle);
        }

        /// <summary>
        ///     Sets the member details.
        /// </summary>
        /// <param name="member">The member.</param>
        public void SetMemberDetails(IClanMember member)
        {
            _memberDetails = member;
            if (member != null)
            {
                _memberJob = member.Job;
                _memberRank = member.Rank;
            }

            RefreshMemberDetails();
        }

        /// <summary>
        ///     Saves the member details.
        /// </summary>
        public void SaveMemberDetails()
        {
            if (_memberDetails != null)
            {
                _memberDetails.Job = _memberJob;
                _memberDetails.Rank = _memberRank;
                // TODO - Send packet to master server and save
            }
        }

        /// <summary>
        ///     Refreshes the member details.
        /// </summary>
        public void RefreshMemberDetails()
        {
            Owner.Configurations.SendGlobalCs2Int(1500, _memberDetails == null ? -1 : (int)_memberDetails.Rank);
            Owner.Configurations.SendGlobalCs2Int(1501, _memberDetails == null ? -1 : (int)_memberDetails.Job);
            Owner.Configurations.SendGlobalCs2Int(1564, _memberDetails == null ? -1 : 0); // dunno
            Owner.Configurations.SendGlobalCs2Int(1565, _memberDetails == null ? -1 : 0); // banned from keep
            Owner.Configurations.SendGlobalCs2Int(1566, _memberDetails == null ? -1 : 0); // banned from citadel
            Owner.Configurations.SendGlobalCs2Int(1567, _memberDetails == null ? -1 : 0); // banned from island
            Owner.Configurations.SendGlobalCs2Int(1568, _memberDetails == null ? -1 : 0); // first week
            Owner.Configurations.SendGlobalCs2String(347, _memberDetails == null ? string.Empty : _memberDetails.DisplayName);
            Owner.Configurations.SendCs2Script(4319, []); // shows slide-up menu
        }

        /// <summary>
        ///     Selects the category.
        /// </summary>
        /// <param name="category">The category.</param>
        public void SelectPermissionCategory(int category) => Owner.Configurations.SendCs2Script(5136, [category]);

        /// <summary>
        ///     Selects the rank.
        /// </summary>
        /// <param name="selectedRank">The selected rank.</param>
        public void SelectPermisionRank(int selectedRank)
        {
            if (selectedRank == 10)
            {
                selectedRank = 125;
            }
            else if (selectedRank > 5)
            {
                selectedRank = 100 + selectedRank - 6;
            }

            Owner.Configurations.SendGlobalCs2Int(1649, 1);
            Owner.Configurations.SendGlobalCs2Int(1792, 1);

            // new in 742+
            Owner.Configurations.SendGlobalCs2Int(2001, 1);
            Owner.Configurations.SendGlobalCs2Int(2002, 1);
            Owner.Configurations.SendGlobalCs2Int(2003, 1);
            // new in 742+

            Owner.Configurations.SendGlobalCs2Int(1569, selectedRank);
            Owner.Configurations.SendGlobalCs2Int(1570, 1);
            Owner.Configurations.SendGlobalCs2Int(1571, 1);
            Owner.Configurations.SendGlobalCs2Int(1572, 1);
            Owner.Configurations.SendGlobalCs2Int(1573, 1);
            Owner.Configurations.SendGlobalCs2Int(1574, 1);
            Owner.Configurations.SendGlobalCs2Int(1575, 1);
            Owner.Configurations.SendGlobalCs2Int(1576, 1);
            Owner.Configurations.SendGlobalCs2Int(1577, 1);
            Owner.Configurations.SendGlobalCs2Int(1578, 1);
            Owner.Configurations.SendGlobalCs2Int(1579, 1);
            Owner.Configurations.SendGlobalCs2Int(1580, 1);
            Owner.Configurations.SendGlobalCs2Int(1581, 1);
            Owner.Configurations.SendGlobalCs2Int(1582, 1);
            Owner.Configurations.SendGlobalCs2Int(1583, 1);
            Owner.Configurations.SendGlobalCs2Int(1584, 1);
            Owner.Configurations.SendGlobalCs2Int(1585, 1);
            Owner.Configurations.SendGlobalCs2Int(1586, 1);
            Owner.Configurations.SendGlobalCs2Int(1587, 1);
            Owner.Configurations.SendGlobalCs2Int(1588, 1);
            Owner.Configurations.SendGlobalCs2Int(1589, 1);
            Owner.Configurations.SendGlobalCs2Int(1590, 1);
        }

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose() => _memberDetails = null;
    }
}