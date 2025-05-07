using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Oziach
{
    /// <summary>
    /// </summary>
    public class OziachDialogue : NpcDialogueScript
    {
        private readonly IItemBuilder _itemBuilder;
        public OziachDialogue(ICharacterContextAccessor characterContextAccessor, IItemBuilder itemBuilder) : base(characterContextAccessor) => _itemBuilder = itemBuilder;

        /// <summary>
        ///     Initializes the dialogue.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0,
                (extraData1, extraData2) =>
                {
                    StandardNpcDialogue(TalkingTo,
                        DialogueAnimations.Angry,
                        "What do you want, " + Owner.DisplayName + "?",
                        "Why are you bothering me?",
                        "If you're selling something",
                        "don't waste your breath!");
                    return true;
                });
            AttachDialogueContinueClickHandler(1,
                (extraData1, extraData2) =>
                {
                    DefaultOptionDialogue("I've only come to ask a favor.", "I'll take my business elsewhere.");
                    return true;
                });
            AttachDialogueOptionClickHandler("I've only come to ask a favor.",
                (extraData1, extraData2) =>
                {
                    DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "I've only come to ask a favor.", "I would like you to create a Dragonfire shield!");
                    SetStage(1);
                    return true;
                });
            AttachDialogueOptionClickHandler("I'll take my business elsewhere.",
                (extraData1, extraData2) =>
                {
                    DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "Very well then.", "I'll take my business elsewhere, Old Man.");
                    SetStage(7);
                    return false;
                });
            AttachDialogueContinueClickHandler(2,
                (extraData1, extraData2) =>
                {
                    StandardNpcDialogue(TalkingTo,
                        DialogueAnimations.CalmTalk,
                        "Oh yeah?...",
                        "I charge 1,250,000 coins for that Dragonfire Shield.",
                        "Do you have all the required materials and my pay?");
                    return true;
                });
            AttachDialogueContinueClickHandler(3,
                (extraData1, extraData2) =>
                {
                    DefaultOptionDialogue("Yes.", "No.");
                    return true;
                });
            AttachDialogueOptionClickHandler("Yes.",
                (extraData1, extraData2) =>
                {
                    if (Stage == 4)
                    {
                        DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "Yes, I have everything you need. Including your pay.");
                        SetStage(3);
                    }

                    return true;
                });
            AttachDialogueOptionClickHandler("No.",
                (extraData1, extraData2) =>
                {
                    if (Stage == 4)
                    {
                        DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "I'm not paying you, you cantankerous old fool!");
                        SetStage(6);
                    }

                    return true;
                });
            AttachDialogueContinueClickHandler(4,
                (extraData1, extraData2) =>
                {
                    if (Owner.MoneyPouch.Contains(995, 1200000) && Owner.Inventory.Contains(11286, 1) && Owner.Inventory.Contains(1540, 1))
                    {
                        var removed = Owner.Inventory.Remove(_itemBuilder.Create().WithId(11286).Build());
                        removed += Owner.Inventory.Remove(_itemBuilder.Create().WithId(1540).Build());
                        removed += Owner.MoneyPouch.Remove(1250000);
                        if (removed >= 3)
                        {
                            Owner.Inventory.Add(_itemBuilder.Create().WithId(11284).Build()); // the not-charged version. the charged version is 11283
                        }

                        StandardNpcDialogue(TalkingTo,
                            DialogueAnimations.CalmTalk,
                            "It was a pleasure doin' business with you.",
                            "Here is your Dragonfire Shield!");
                    }
                    else
                    {
                        StandardNpcDialogue(TalkingTo, DialogueAnimations.Mad, "You're missing materials, fool!");
                        SetStage(7);
                    }

                    return true;
                });
            AttachDialogueContinueClickHandler(5,
                (extraData1, extraData2) =>
                {
                    DefaultCharacterDialogue(DialogueAnimations.CalmTalk, "Thank you very much!");
                    return true;
                });
            AttachDialogueContinueClickHandler(6,
                (extraData1, extraData2) =>
                {
                    StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "No problem! Come back if you want one more!");
                    SetStage(7);
                    return true;
                });
            AttachDialogueContinueClickHandler(7,
                (extraData1, extraData2) =>
                {
                    StandardNpcDialogue(TalkingTo, DialogueAnimations.Angry, "Why you little... Go away you swine!");
                    return true;
                });
            AttachDialogueContinueClickHandler(8,
                (extraData1, extraData2) =>
                {
                    Owner.Widgets.CloseChatboxOverlay();
                    return true;
                });
        }
    }
}