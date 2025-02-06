using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Widgets.Skills;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    ///     Represents skills tab.
    /// </summary>
    public class SkillsTab : WidgetScript
    {
        /// <summary>
        ///     The component ids
        /// </summary>
        private static readonly int[] _componentIDs = [150, 22, 9, 145, 40, 58, 71, 52, 125, 77, 34, 130, 64, 135, 140, 28, 15, 46, 90, 96, 84, 102, 108, 114, 120
        ];

        /// <summary>
        ///     Contains set target handler.
        /// </summary>
        private OnIntInput? _setTargetHandler;

        public SkillsTab(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            Refresh();

            foreach (var t in _componentIDs)
            {
                InterfaceInstance.AttachClickHandler(t, SkillClicked);
            }
        }

        /// <summary>
        ///     Happens when skill button is clicked.
        /// </summary>
        /// <returns></returns>
        public bool SkillClicked(int componentID, ComponentClickType clickType, int extraData1, int extraData2)
        {
            var skillID = GetSkillID(componentID);
            if (skillID >= byte.MaxValue)
            {
                return false;
            }

            if (clickType == ComponentClickType.LeftClick)
            {
                if (Owner.Statistics.SkillFlashed(skillID)) // levelup interface
                {
                    if (Owner.IsBusy())
                    {
                        Owner.SendChatMessage("Please finish what you are doing before opening skills interface.");
                        return true;
                    }

                    Owner.Statistics.StopFlashingSkill(skillID);
                    var levelupScript = Owner.ServiceProvider.GetRequiredService<LevelupInterface>();
                    Owner.Widgets.OpenWidget(741, 0, levelupScript, true);
                    levelupScript.Setup(skillID);
                    return true;
                }
                else // features interface
                {
                    if (Owner.IsBusy())
                    {
                        Owner.SendChatMessage("Please finish what you are doing before opening skills interface.");
                        return true;
                    }

                    var featuresInterface = Owner.ServiceProvider.GetRequiredService<FeaturesInterface>();
                    Owner.Widgets.OpenWidget(499, 0, featuresInterface, true);
                    featuresInterface.Setup(skillID);
                    return true;
                }
            }

            if (clickType == ComponentClickType.Option2Click) // set level target
            {
                if (Owner.IsBusy())
                {
                    Owner.SendChatMessage("Please finish what you are doing before setting a level target.");
                    return true;
                }

                _setTargetHandler = Owner.Widgets.IntInputHandler = value =>
                {
                    _setTargetHandler = Owner.Widgets.IntInputHandler = null;
                    if (value <= 0)
                    {
                        Owner.SendChatMessage("Value can't be negative.");
                    }
                    else
                    {
                        if (value <= Owner.Statistics.LevelForExperience(skillID))
                        {
                            var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultDialogueScript>();
                            if (Owner.Widgets.OpenDialogue(defaultScript, true))
                            {
                                defaultScript.AttachDialogueContinueClickHandler(0, (extra1, extra2) => Owner.Widgets.CloseChatboxOverlay());
                                defaultScript.StandardDialogue("You must set a target higher than your current skill level.");
                            }
                        }
                        else if (skillID != 24 && value > 99 || skillID == 24 && value > 120)
                        {
                            var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultDialogueScript>();
                            if (Owner.Widgets.OpenDialogue(defaultScript, true))
                            {
                                defaultScript.AttachDialogueContinueClickHandler(0, (extra1, extra2) => Owner.Widgets.CloseChatboxOverlay());
                                defaultScript.StandardDialogue("You cannot set a level target higher than " + (skillID == 24 ? 120 : 99) + ".");
                            }
                        }
                        else
                        {
                            Owner.Statistics.SetSkillTargetLevel(skillID, value);
                        }
                    }
                };
                Owner.Configurations.SendIntegerInput("What level target do you wish to set?");
                return true;
            }

            if (clickType == ComponentClickType.Option3Click) // set xp target
            {
                if (Owner.IsBusy())
                {
                    Owner.SendChatMessage("Please finish what you are doing before setting a XP target.");
                    return true;
                }

                _setTargetHandler = Owner.Widgets.IntInputHandler = value =>
                {
                    _setTargetHandler = Owner.Widgets.IntInputHandler = null;
                    if (value <= 0)
                    {
                        Owner.SendChatMessage("Value can't be negative.");
                    }
                    else
                    {
                        if (value <= Owner.Statistics.GetSkillExperience(skillID))
                        {
                            var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultDialogueScript>();
                            if (Owner.Widgets.OpenDialogue(defaultScript, true))
                            {
                                defaultScript.AttachDialogueContinueClickHandler(0, (extra1, extra2) => Owner.Widgets.CloseChatboxOverlay());
                                defaultScript.StandardDialogue("You must set a target higher than your current XP.");
                            }
                        }
                        else if (value > StatisticsConstants.MaximumExperience)
                        {
                            var defaultScript = Owner.ServiceProvider.GetRequiredService<DefaultDialogueScript>();
                            if (Owner.Widgets.OpenDialogue(defaultScript, true))
                            {
                                defaultScript.AttachDialogueContinueClickHandler(0, (extra1, extra2) => Owner.Widgets.CloseChatboxOverlay());
                                defaultScript.StandardDialogue("That number is larger than the amount of XP it is possible to attain.");
                            }
                        }
                        else
                        {
                            Owner.Statistics.SetSkillTargetExperience(skillID, value);
                        }
                    }
                };
                Owner.Configurations.SendIntegerInput("What XP target do you wish to set?");
                return true;
            }

            if (clickType == ComponentClickType.Option4Click) // clear target
            {
                Owner.Statistics.SetSkillTargetLevel(skillID, -1);
                Owner.Statistics.SetSkillTargetExperience(skillID, -1);
                return true;
            }

            return false;
        }


        /// <summary>
        ///     Refreshe's character skills.
        /// </summary>
        public void Refresh()
        {
            Owner.Statistics.RefreshSkills();
            Owner.Statistics.RefreshSkillTargets();
        }

        /// <summary>
        ///     Get's skill Id for component Id.
        /// </summary>
        /// <param name="componentID"></param>
        /// <returns></returns>
        public static byte GetSkillID(int componentID)
        {
            for (byte i = 0; i < _componentIDs.Length; i++)
            {
                if (_componentIDs[i] == componentID)
                {
                    return i;
                }
            }

            return byte.MaxValue;
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
            if (_setTargetHandler != null)
            {
                if (Owner.Widgets.IntInputHandler == _setTargetHandler)
                {
                    Owner.Widgets.IntInputHandler = null;
                }

                _setTargetHandler = null;
            }
        }
    }
}