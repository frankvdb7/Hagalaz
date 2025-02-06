using System;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Skills.Magic.TeleportSpells;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Mandrith
{
    /// <summary>
    ///     Contains Mandrith Dialogue.
    /// </summary>
    public class MandrithDialogue : NpcDialogueScript
    {
        /// <summary>
        ///     The next callback.
        /// </summary>
        private Action _moreOptionsCallback;

        /// <summary>
        ///     The back callback.
        /// </summary>
        private Action _backCallback;

        public MandrithDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen() => Setup();

        /// <summary>
        ///     Setups this instance.
        /// </summary>
        public void Setup()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Hello " + Owner.DisplayName + "!", "Where do you want to go?");
                return true;
            });
            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Training", "Skills", "Dungeons", "Minigames", "More Options");
                _moreOptionsCallback = () =>
                {
                    _backCallback = () => OnDialogueContinueClick(1, -1, -1);
                    DefaultOptionDialogue("Guilds", "Back");
                };
                return true;
            });
            AttachDialogueOptionClickHandler("Training", (extraData1, extraData2) =>
            {
                new TrainingTeleport().PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Skills", (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Agility", "Farming", "Slayer", "Thieving", "Back");
                _backCallback = () => OnDialogueContinueClick(1, -1, -1);
                return false;
            });
            AttachDialogueOptionClickHandler("Dungeons", (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Godwars Dungeon", "Forinthry Dungeon", "Glacor Cave", "More Options", "Back");
                _backCallback = () => OnDialogueContinueClick(1, -1, -1);
                _moreOptionsCallback = () =>
                {
                    DefaultOptionDialogue("Ancient Guthix Temple", "Living Rock Cavern", "Ancient Cavern", "More Options", "Back");
                    _moreOptionsCallback = () =>
                    {
                        DefaultOptionDialogue("Waterbirth Island Dungeon", "Jadinko Lair", "Grotworm lair", "More Options", "Back");
                        _moreOptionsCallback = () =>
                        {
                            DefaultOptionDialogue("Kalphite Hive", "Back");
                        };
                    };
                };
                return false;
            });
            AttachDialogueOptionClickHandler("Minigames", (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Barrows", "Tzhaar City", "Duel Arena", "Back");
                _backCallback = () => OnDialogueContinueClick(1, -1, -1);
                return false;
            });
            AttachDialogueOptionClickHandler("Other", (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("", "Back");
                return false;
            });
            AttachDialogueOptionClickHandler("Back", (extraData1, extraData2) =>
            {
                if (_backCallback != null)
                {
                    _backCallback.Invoke();
                }

                return false;
            });
            AttachDialogueOptionClickHandler("More Options", (extraData1, extraData2) =>
            {
                if (_moreOptionsCallback != null)
                {
                    _moreOptionsCallback.Invoke();
                }

                return false;
            });
            AttachDialogueOptionClickHandler("Agility", (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Gnome Agility Course", "Back");
                return false;
            });
            AttachDialogueOptionClickHandler("Farming", (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Falador Allotment", "Catherby Allotment", "Back");
                return false;
            });
            AttachDialogueOptionClickHandler("Slayer", (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Slayer Tower", "Back");
                return false;
            });
            AttachDialogueOptionClickHandler("Thieving", (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Rogue's Den", "Back");
                return false;
            });
            AttachDialogueOptionClickHandler("Gnome Agility Course", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 2470, 3438, 0, 2).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Godwars Dungeon", (extraData1, extraData2) =>
            {
                new GodwarsTeleport().PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Guilds", (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Warrior Guild", "Crafting Guild", "Back");
                _backCallback = () => OnDialogueContinueClick(1, -1, -1);
                return false;
            });
            AttachDialogueOptionClickHandler("Warrior Guild", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 2859, 3542, 2, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Rogue's Den", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 3047, 4971, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Ranging Guild", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 2656, 3440, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Mage Guild", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 2600, 3087, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Crafting Guild", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 2933, 3291, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Heroes Guild", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 2906, 3511, 0, 3).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Champions Guild", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 3191, 3368, 0, 3).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Legends Guild", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 2728, 3347, 0, 2).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Fishing Guild", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 2614, 3382, 0, 3).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Mining Guild", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 3031, 3336, 0, 2).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Falador Allotment", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 3051, 3304, 0, 2).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Catherby Allotment", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 2802, 3463, 0, 3).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Runecrafting Guild", (extraData1, extraData2) =>
            {
                if (Owner.Statistics.GetSkillLevel(StatisticsConstants.Runecrafting) >= 50)
                {
                    new StandardTeleportScript(MagicBook.StandardBook, 1696, 5460, 2, 0).PerformTeleport(Owner);
                    Owner.Widgets.CloseChatboxOverlay();
                }
                else
                {
                    StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "You need a Runecrafting level of 50 in order to teleport to the Runecrafting guild.");
                }

                return false;
            });
            AttachDialogueOptionClickHandler("Slayer Tower", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 3429, 3538, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("King Black Dragon", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 3067, 10254, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Ancient Cavern", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 1763, 5365, 1, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Living Rock Cavern", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 3651, 5122, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Ancient Guthix Temple", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 2525, 5810, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Waterbirth Island Dungeon", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 2900, 4449, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Kalphite Hive", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 3479, 9516, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Jadinko Lair", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 3012, 9275, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Grotworm lair", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 1206, 6372, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Glacor Cave", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 4196, 5755, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
            AttachDialogueOptionClickHandler("Barrows", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 3565, 3313, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
            AttachDialogueOptionClickHandler("Duel Arena", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 3363, 3276, 0, 3).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
            AttachDialogueOptionClickHandler("Tzhaar City", (extraData1, extraData2) =>
            {
                new StandardTeleportScript(MagicBook.StandardBook, 4668, 5059, 0, 0).PerformTeleport(Owner);
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
            AttachDialogueOptionClickHandler("Forinthry Dungeon", (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Are you sure you want to go to Forinthry Dungeon?", "It is very dangerous place and you are likely to die around there.");
                return false;
            });
            AttachDialogueContinueClickHandler(2, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Yes", "No");
                return true;
            });
            AttachDialogueOptionClickHandler("Yes", (extraData1, extraData2) =>
            {
                if (Stage == 3)
                {
                    new StandardTeleportScript(MagicBook.StandardBook, 3077, 10058, 0, 0).PerformTeleport(Owner);
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                }

                return false;
            });
            AttachDialogueOptionClickHandler("No", (extraData1, extraData2) =>
            {
                if (Stage == 3)
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                }

                return false;
            });
            AttachDialogueOptionClickHandler("Nevermind", (extraData1, extraData2) =>
            {
                if (Stage == 2)
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                }

                return false;
            });
        }
    }
}