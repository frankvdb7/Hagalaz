using Hagalaz.Configuration;
using Hagalaz.Game.Abstractions.Features.FriendsChat;
using Hagalaz.Game.Abstractions.Mediator;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters.Actions;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.FriendsChat
{
    /// <summary>
    ///     The friends chat channel setup interface. This interface allows you to edit rights in the chat channel.
    /// </summary>
    public class FriendsChatSetup : WidgetScript
    {
        /// <summary>
        ///     Contains the prefex input.
        /// </summary>
        private OnStringInput? _prefexInput;
        private readonly IScopedGameMediator _gameMediator;

        public FriendsChatSetup(ICharacterContextAccessor characterContextAccessor, IScopedGameMediator gameMediator) : base(characterContextAccessor)
        {
            _gameMediator = gameMediator;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            // setup settings.
            SetFriendsChatSettings();

            // clan name/alias setting.
            InterfaceInstance.AttachClickHandler(1, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick) // set prefix
                {
                    _prefexInput = Owner.Widgets.StringInputHandler = SetAlias;
                    Owner.Configurations.SendStringInput("Enter chat prefix:");
                    return true;
                }

                if (clickType == ComponentClickType.Option2Click) // disable
                {
                    SetAlias("");
                    return true;
                }

                return false;
            });

            // "who can enter chat?" setting
            InterfaceInstance.AttachClickHandler(2, (componentID, clickType, extraData1, extraData2) =>
            {
                var rank = FriendsChatRank.Regular;
                if (clickType == ComponentClickType.LeftClick)
                {
                    rank = FriendsChatRank.Regular;
                }
                else if (clickType == ComponentClickType.Option2Click)
                {
                    rank = FriendsChatRank.Friend;
                }
                else if (clickType == ComponentClickType.Option3Click)
                {
                    rank = FriendsChatRank.Recruit;
                }
                else if (clickType == ComponentClickType.Option4Click)
                {
                    rank = FriendsChatRank.Corporal;
                }
                else if (clickType == ComponentClickType.Option5Click)
                {
                    rank = FriendsChatRank.Sergeant;
                }
                else if (clickType == ComponentClickType.Option6Click)
                {
                    rank = FriendsChatRank.Lieutenant;
                }
                else if (clickType == ComponentClickType.Option7Click)
                {
                    rank = FriendsChatRank.Captain;
                }
                else if (clickType == ComponentClickType.Option8Click)
                {
                    rank = FriendsChatRank.General;
                }
                else if (clickType == ComponentClickType.Option9Click)
                {
                    rank = FriendsChatRank.Owner;
                }

                InterfaceInstance.DrawString(2, GetRankString(rank));
                SetRank("fc_rank_enter", rank);
                return true;
            });

            // "who can talk on chat?" setting
            InterfaceInstance.AttachClickHandler(3, (componentID, clickType, extraData1, extraData2) =>
            {
                var rank = FriendsChatRank.Regular;
                if (clickType == ComponentClickType.LeftClick)
                {
                    rank = FriendsChatRank.Regular;
                }
                else if (clickType == ComponentClickType.Option2Click)
                {
                    rank = FriendsChatRank.Friend;
                }
                else if (clickType == ComponentClickType.Option3Click)
                {
                    rank = FriendsChatRank.Recruit;
                }
                else if (clickType == ComponentClickType.Option4Click)
                {
                    rank = FriendsChatRank.Corporal;
                }
                else if (clickType == ComponentClickType.Option5Click)
                {
                    rank = FriendsChatRank.Sergeant;
                }
                else if (clickType == ComponentClickType.Option6Click)
                {
                    rank = FriendsChatRank.Lieutenant;
                }
                else if (clickType == ComponentClickType.Option7Click)
                {
                    rank = FriendsChatRank.Captain;
                }
                else if (clickType == ComponentClickType.Option8Click)
                {
                    rank = FriendsChatRank.General;
                }
                else if (clickType == ComponentClickType.Option9Click)
                {
                    rank = FriendsChatRank.Owner;
                }

                InterfaceInstance.DrawString(3, GetRankString(rank));
                SetRank("fc_rank_talk", rank);
                return true;
            });

            // "who can kick from chat?" setting
            InterfaceInstance.AttachClickHandler(4, (componentID, clickType, extraData1, extraData2) =>
            {
                var rank = FriendsChatRank.Owner;
                if (clickType == ComponentClickType.Option4Click)
                {
                    rank = FriendsChatRank.Corporal;
                }
                else if (clickType == ComponentClickType.Option5Click)
                {
                    rank = FriendsChatRank.Sergeant;
                }
                else if (clickType == ComponentClickType.Option6Click)
                {
                    rank = FriendsChatRank.Lieutenant;
                }
                else if (clickType == ComponentClickType.Option7Click)
                {
                    rank = FriendsChatRank.Captain;
                }
                else if (clickType == ComponentClickType.Option8Click)
                {
                    rank = FriendsChatRank.General;
                }
                else if (clickType == ComponentClickType.Option9Click)
                {
                    rank = FriendsChatRank.Owner;
                }

                InterfaceInstance.DrawString(4, GetRankString(rank));
                SetRank("fc_rank_kick", rank);
                return true;
            });

            // "who can loot share?" setting
            InterfaceInstance.AttachClickHandler(5, (componentID, clickType, extraData1, extraData2) =>
            {
                var rank = FriendsChatRank.NoOne;
                if (clickType == ComponentClickType.LeftClick)
                {
                    rank = FriendsChatRank.NoOne;
                }
                else if (clickType == ComponentClickType.Option2Click)
                {
                    rank = FriendsChatRank.Friend;
                }
                else if (clickType == ComponentClickType.Option3Click)
                {
                    rank = FriendsChatRank.Recruit;
                }
                else if (clickType == ComponentClickType.Option4Click)
                {
                    rank = FriendsChatRank.Corporal;
                }
                else if (clickType == ComponentClickType.Option5Click)
                {
                    rank = FriendsChatRank.Sergeant;
                }
                else if (clickType == ComponentClickType.Option6Click)
                {
                    rank = FriendsChatRank.Lieutenant;
                }
                else if (clickType == ComponentClickType.Option7Click)
                {
                    rank = FriendsChatRank.Captain;
                }
                else if (clickType == ComponentClickType.Option8Click)
                {
                    rank = FriendsChatRank.General;
                }

                InterfaceInstance.DrawString(5, GetRankString(rank));
                SetRank("fc_rank_loot", rank);
                return true;
            });

            InterfaceInstance.AttachClickHandler(12, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _gameMediator.Publish(new ProfileToggleBoolAction(ProfileConstants.FriendsChatSettingsLootShareToggled));
                    return true;
                }

                return false;
            });
        }

        /// <summary>
        ///     Gets the friends chat settings shows them on the interface.
        /// </summary>
        public void SetFriendsChatSettings()
        {
            // channel name/alias
            var alias = Owner.Profile.GetValue(ProfileConstants.FriendsChatSettingsAlias);
            InterfaceInstance.DrawString(1, string.IsNullOrEmpty(alias) ? "Disabled" : alias);

            // rank requirements
            InterfaceInstance.DrawString(2, GetRankString(Owner.Profile.GetValue<FriendsChatRank>(ProfileConstants.FriendsChatSettingsEnterRank)));
            InterfaceInstance.DrawString(3, GetRankString(Owner.Profile.GetValue<FriendsChatRank>(ProfileConstants.FriendsChatSettingsTalkRank)));
            InterfaceInstance.DrawString(4, GetRankString(Owner.Profile.GetValue<FriendsChatRank>(ProfileConstants.FriendsChatSettingsKickRank)));
            InterfaceInstance.DrawString(5, GetRankString(Owner.Profile.GetValue<FriendsChatRank>(ProfileConstants.FriendsChatSettingsLootRank)));
        }

        /// <summary>
        ///     Gets the correct rank as a string for displaying on interface.
        /// </summary>
        /// <param name="rank">The rank to get string for.</param>
        /// <returns>Returns a string.</returns>
        public static string GetRankString(FriendsChatRank rank)
        {
            switch (rank)
            {
                case FriendsChatRank.NoOne: return "No-one";
                case FriendsChatRank.Regular: return "Anyone";
                case FriendsChatRank.Friend: return "Any friends";
                case FriendsChatRank.Recruit: return "Recruit+";
                case FriendsChatRank.Corporal: return "Corporal+";
                case FriendsChatRank.Sergeant: return "Sergeant+";
                case FriendsChatRank.Lieutenant: return "Lieutenant+";
                case FriendsChatRank.Captain: return "Captain+";
                case FriendsChatRank.General: return "General+";
                case FriendsChatRank.Owner: return "Only me";
            }

            return "";
        }

        /// <summary>
        ///     Sets the character's clan alias name.
        ///     By setting the clan alias, the character's friends chat channel is automatically enabled.
        /// </summary>
        /// <param name="alias">The clan name.</param>
        public void SetAlias(string alias)
        {
            _prefexInput = Owner.Widgets.StringInputHandler = null;
            InterfaceInstance.DrawString(1, alias == string.Empty ? "Disabled" : alias);
            _gameMediator.Publish(new ProfileSetStringAction(ProfileConstants.FriendsChatSettingsAlias, alias));
        }

        /// <summary>
        ///     Set a rank in database.
        /// </summary>
        /// <param name="profileKey'">The name of the rank database column.</param>
        /// <param name="rank">The rank to be set.</param>
        public void SetRank(string profileKey, FriendsChatRank rank) => _gameMediator.Publish(new ProfileSetEnumAction(profileKey, rank));

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