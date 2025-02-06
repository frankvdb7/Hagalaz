using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue
{
    /// <summary>
    /// </summary>
    public class SkillCapeDialogue : NpcDialogueScript
    {
        /// <summary>
        ///     The skill Id.
        /// </summary>
        private readonly int _skillID;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SkillCapeDialogue" /> class.
        /// </summary>
        /// <param name="skillID">The skill Id.</param>
        public SkillCapeDialogue(ICharacterContextAccessor contextAccessor, int skillID = 0) : base(contextAccessor) => _skillID = skillID;

        /// <summary>
        ///     The SKIL l_ CAPES
        /// </summary>
        public static readonly short[] SkillCapes =
        [
            9747, // Attack
            9753, // Defence
            9750, // Strength
            9768, // Hitpoints
            9756, // Ranging
            9759, // Prayer
            9762, // Magic
            9801, // Cooking
            9807, // Woodcutting
            9783, // Fletching
            9798, // Fishing
            9804, // Firemaking
            9780, // Crafting
            9795, // Smithing
            9792, // Mining
            9774, // Herblore
            9771, // Agility
            9777, // Thieving
            9786, // Slayer
            9810, // Farming
            9765, // Runecrafting
            9789, // Construction
            9948, // Hunter
            12169, // Summoning
            18508 // Dungeoneering
        ];

        /// <summary>
        ///     The t_ skil l_ capes
        /// </summary>
        public static readonly short[] TSkillCapes =
        [
            9748, // Attack
            9754, // Defence
            9751, // Strength
            9769, // Hitpoints
            9757, // Ranging
            9760, // Prayer
            9763, // Magic
            9802, // Cooking
            9808, // Woodcutting
            9784, // Fletching
            9799, // Fishing
            9805, // Firemaking
            9781, // Crafting
            9796, // Smithing
            9793, // Mining
            9775, // Herblore
            9772, // Agility
            9778, // Thieving
            9787, // Slayer
            9811, // Farming
            9766, // Runecrafting
            9790, // Construction
            9949, // Hunter
            12170, // Summoning
            18509 // Dungeoneering
        ];

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
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Hello " + Owner.DisplayName + "!", "How can I help you?");
                return true;
            });
            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "I'd like to talk about skill capes please!");
                return true;
            });
            AttachDialogueContinueClickHandler(2, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "The " + StatisticsConstants.SkillNames[_skillID] + " skill cape costs 99,000 coins and requires level 99 in " + StatisticsConstants.SkillNames[_skillID] + ". A skill cape is something you can show off to other players!", "Would you like to buy this cape from me?");
                return true;
            });
            AttachDialogueContinueClickHandler(3, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Yes", "No");
                return true;
            });
            AttachDialogueOptionClickHandler("Yes", (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "Yes please!");
                SetStage(6);
                return false;
            });
            AttachDialogueOptionClickHandler("No", (extraData1, extraData2) =>
            {
                DefaultCharacterDialogue(DialogueAnimations.Sad, "No thanks.");
                SetStage(4);
                return false;
            });
            AttachDialogueContinueClickHandler(4, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Alright, see you again soon!");
                return true;
            });
            AttachDialogueContinueClickHandler(5, (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueContinueClickHandler(6, (extraData1, extraData2) =>
            {
                if (Owner.Statistics.LevelForExperience(_skillID) >= 99)
                {
                    if (Owner.MoneyPouch.Contains(995, 99000))
                    {
                        var removed = Owner.MoneyPouch.Remove(99000);
                        if (removed > 0 && Owner.Inventory.FreeSlots >= 2)
                        {
                            var hasTwo99 = false;
                            for (var skill = 0; !hasTwo99 && skill < NpcStatisticsConstants.SkillsCount; skill++)
                            {
                                if (skill != _skillID)
                                {
                                    if (Owner.Statistics.LevelForExperience(skill) >= 99)
                                    {
                                        hasTwo99 = true;
                                    }
                                }
                            }

                            Owner.Inventory.Add(new Item(hasTwo99 ? TSkillCapes[_skillID] : SkillCapes[_skillID], 1));
                            Owner.Inventory.Add(new Item((short)(SkillCapes[_skillID] + 2), 1));
                            StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Congratulations! You have just bought a " + StatisticsConstants.SkillNames[_skillID] + " Skill Cape " + (hasTwo99 ? "(T)" : "") + "!");
                        }
                        else
                        {
                            StandardNpcDialogue(TalkingTo, DialogueAnimations.Mad, "Make some room in your inventory first, will ya?");
                        }
                    }
                    else
                    {
                        StandardNpcDialogue(TalkingTo, DialogueAnimations.Mad, "You do not have enough coins to buy this skill cape!");
                    }
                }
                else
                {
                    StandardNpcDialogue(TalkingTo, DialogueAnimations.Mad, "You need a(n) " + StatisticsConstants.SkillNames[_skillID] + " of 99 to buy this skill cape!");
                }

                return true;
            });

            AttachDialogueContinueClickHandler(7, (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });
        }
    }
}