namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// Provides constant values related to dialogues and widgets.
    /// </summary>
    public static class DialogueConstants
    {
        /// <summary>
        /// The default information string for "make-x" crafting dialogues.
        /// </summary>
        public const string DefaultInfoString = "Choose how many you wish to make, <br>then click on the chosen item to begin.";
    }

    /// <summary>
    /// Defines the widget IDs for various dialogue interfaces.
    /// </summary>
    public enum DialogueInterfaces : int
    {
        /// <summary>
        /// A standard dialogue box.
        /// </summary>
        StandardDialogue = 1186,
        /// <summary>
        /// A standard dialogue box with an NPC's head model.
        /// </summary>
        StandardNpcDialogue = 1184,
        /// <summary>
        /// A standard dialogue box with the player character's head model.
        /// </summary>
        StandardCharacterDialogue = 1191,
        /// <summary>
        /// A standard dialogue box with multiple choice options.
        /// </summary>
        StandardOptionsDialogue = 1188,
        /// <summary>
        /// The confirmation box for destroying an item.
        /// </summary>
        DestroyItemBox = 1183,
        /// <summary>
        /// The interface displayed when a skill levels up.
        /// </summary>
        SkillLevelupBox = 740,
        /// <summary>
        /// An interactive chatbox, often used for "make-x" dialogues.
        /// </summary>
        InteractiveChatBox = 905,
        /// <summary>
        /// An interactive box for selecting an amount.
        /// </summary>
        InteractiveSelectAmountBox = 916,
        /// <summary>
        /// A single-line text box on the right side of the chat.
        /// </summary>
        Send1TextChatRight = 64,
        /// <summary>
        /// A two-line text box on the right side of the chat.
        /// </summary>
        Send2TextChatRight = 65,
        /// <summary>
        /// A three-line text box on the right side of the chat.
        /// </summary>
        Send3TextChatRight = 66,
        /// <summary>
        /// A four-line text box on the right side of the chat.
        /// </summary>
        Send4TextChatRight = 67,
        /// <summary>
        /// A single-line informational text box.
        /// </summary>
        Send1TextInfo = 210,
        /// <summary>
        /// A two-line informational text box.
        /// </summary>
        Send2TextInfo = 211,
        /// <summary>
        /// A three-line informational text box.
        /// </summary>
        Send3TextInfo = 212,
        /// <summary>
        /// A four-line informational text box.
        /// </summary>
        Send4TextInfo = 213,
        /// <summary>
        /// A two-option choice dialogue.
        /// </summary>
        Send2Option = 229,
        /// <summary>
        /// A three-option choice dialogue.
        /// </summary>
        Send3Option = 230,
        /// <summary>
        /// A four-option choice dialogue.
        /// </summary>
        Send4Option = 233,
        /// <summary>
        /// A five-option choice dialogue.
        /// </summary>
        Send5Option = 234,
        /// <summary>
        /// A two-option choice dialogue.
        /// </summary>
        Send2Options = 236,
        /// <summary>
        /// A four-option choice dialogue.
        /// </summary>
        Send4Options = 237,
        /// <summary>
        /// A five-option choice dialogue.
        /// </summary>
        Send5Options = 238,
        /// <summary>
        /// A two-option choice dialogue with large text.
        /// </summary>
        Send2LargeOptions = 229,
        /// <summary>
        /// A three-option choice dialogue with large text.
        /// </summary>
        Send3LargeOptions = 231,
        /// <summary>
        /// A single-line text box on the left side of the chat.
        /// </summary>
        Send1TextChatLeft = 241,
        /// <summary>
        /// A two-line text box on the left side of the chat.
        /// </summary>
        Send2TextChatLeft = 242,
        /// <summary>
        /// A three-line text box on the left side of the chat.
        /// </summary>
        Send3TextChatLeft = 243,
        /// <summary>
        /// A four-line text box on the left side of the chat.
        /// </summary>
        Send4TextChatLeft = 244,
        /// <summary>
        /// A single-line text box without a "continue" button.
        /// </summary>
        SendNoContinue1TextChat = 245,
        /// <summary>
        /// A two-line text box without a "continue" button.
        /// </summary>
        SendNoContinue2TextChat = 246,
        /// <summary>
        /// A three-line text box without a "continue" button.
        /// </summary>
        SendNoContinue3TextChat = 247,
        /// <summary>
        /// A four-line text box without a "continue" button.
        /// </summary>
        SendNoContinue4TextChat = 248,
        /// <summary>
        /// The dialogue for founding a clan.
        /// </summary>
        FoundClanDialog = 1094,
        /// <summary>
        /// The dialogue for setting a clan motto.
        /// </summary>
        ClanMottoDialog = 1103,
    }

    /// <summary>
    /// Defines the animation IDs for different character expressions in dialogues.
    /// </summary>
    public enum DialogueAnimations : short
    {
        /// <summary>
        /// No animation.
        /// </summary>
        NoAnimation = -1,
        /// <summary>
        /// A calm talking animation.
        /// </summary>
        CalmTalk = 9760,
        /// <summary>
        /// A crying animation.
        /// </summary>
        Crying = 9765,
        /// <summary>
        /// A worried animation.
        /// </summary>
        Worried = 9770,
        /// <summary>
        /// A sad animation.
        /// </summary>
        Sad = 9775,
        /// <summary>
        /// A scared animation.
        /// </summary>
        Scared = 9780,
        /// <summary>
        /// A mad animation.
        /// </summary>
        Mad = 9785,
        /// <summary>
        /// An angry animation.
        /// </summary>
        Angry = 9790,
        /// <summary>
        /// A "crazy eyes" animation.
        /// </summary>
        CrazyEye = 9795,
        /// <summary>
        /// A secondary "crazy eyes" animation.
        /// </summary>
        CrazyEyeSecondary = 9800,
        /// <summary>
        /// An animation for having nothing to say.
        /// </summary>
        NothingToSay = 9805,
        /// <summary>
        /// A staring animation.
        /// </summary>
        Staring = 9810,
        /// <summary>
        /// A "yeah" or nodding animation.
        /// </summary>
        Yeah = 9815,
        /// <summary>
        /// A disgusted animation.
        /// </summary>
        Disgusted = 9820,
        /// <summary>
        /// A "no way" or shaking head animation.
        /// </summary>
        NoWay = 9823,
        /// <summary>
        /// A confused animation.
        /// </summary>
        Confused = 9827,
        /// <summary>
        /// A drunk animation.
        /// </summary>
        Drunk = 9835,
        /// <summary>
        /// A laughing animation.
        /// </summary>
        Laughing = 9840,
        /// <summary>
        /// A head swaying animation.
        /// </summary>
        HeadSway = 9845,
        /// <summary>
        /// A stiff-faced animation.
        /// </summary>
        StiffFace = 9855,
        /// <summary>
        /// A secondary stiff-faced animation.
        /// </summary>
        StiffFaceSecondary = 9860,
        /// <summary>
        /// A prideful animation.
        /// </summary>
        Pridefull = 9865,
        /// <summary>
        /// A demented animation.
        /// </summary>
        Demented = 9870
    }

    /// <summary>
    /// Defines the options available in interactive skill-based dialogues.
    /// </summary>
    public enum InteractiveDialogueOptions : byte
    {
        /// <summary>
        /// The "Make" option.
        /// </summary>
        Make = 0,
        /// <summary>
        /// The "Make Sets" option.
        /// </summary>
        MakeSets = 1,
        /// <summary>
        /// The "Cook" option.
        /// </summary>
        Cook = 2,
        /// <summary>
        /// The "Roast" option.
        /// </summary>
        Roast = 3,
        /// <summary>
        /// The "Offer" option.
        /// </summary>
        Offer = 4,
        /// <summary>
        /// The "Sell" option.
        /// </summary>
        Sell = 5,
        /// <summary>
        /// The "Bake" option.
        /// </summary>
        Bake = 6,
        /// <summary>
        /// The "Cut" option.
        /// </summary>
        Cut = 7,
        /// <summary>
        /// The "Deposit" option.
        /// </summary>
        Deposit = 8,
        /// <summary>
        /// A "Make" option without "All" or "Custom" choices.
        /// </summary>
        MakeNoAllNoCustom = 9,
        /// <summary>
        /// The "Teleport" option.
        /// </summary>
        Teleport = 10,
        /// <summary>
        /// The "Select" option.
        /// </summary>
        Select = 11,
        /// <summary>
        /// The "Take" option.
        /// </summary>
        Take = 13
    }
}