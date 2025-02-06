namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// Simple static class for holding dialogue constants.
    /// </summary>
    public static class DialogueConstants
    {
        /// <summary>
        /// The default information string.
        /// </summary>
        public const string DefaultInfoString = "Choose how many you wish to make, <br>then click on the chosen item to begin.";
    }

    /// <summary>
    /// Contains dialogue interfaces.
    /// </summary>
    public enum DialogueInterfaces : int
    {
        /// <summary>
        /// 
        /// </summary>
        StandardDialogue = 1186,

        /// <summary>
        /// 
        /// </summary>
        StandardNpcDialogue = 1184,

        /// <summary>
        /// 
        /// </summary>
        StandardCharacterDialogue = 1191,

        /// <summary>
        /// 
        /// </summary>
        StandardOptionsDialogue = 1188,

        /// <summary>
        /// 
        /// </summary>
        DestroyItemBox = 1183,

        /// <summary>
        /// 
        /// </summary>
        SkillLevelupBox = 740,

        /// <summary>
        /// 
        /// </summary>
        InteractiveChatBox = 905,

        /// <summary>
        /// 
        /// </summary>
        InteractiveSelectAmountBox = 916,

        /// <summary>
        /// 
        /// </summary>
        Send1TextChatRight = 64,

        /// <summary>
        /// 
        /// </summary>
        Send2TextChatRight = 65,

        /// <summary>
        /// 
        /// </summary>
        Send3TextChatRight = 66,

        /// <summary>
        /// 
        /// </summary>
        Send4TextChatRight = 67,

        /// <summary>
        /// 
        /// </summary>
        Send1TextInfo = 210,

        /// <summary>
        /// 
        /// </summary>
        Send2TextInfo = 211,

        /// <summary>
        /// 
        /// </summary>
        Send3TextInfo = 212,

        /// <summary>
        /// 
        /// </summary>
        Send4TextInfo = 213,

        /// <summary>
        /// 
        /// </summary>
        Send2Option = 229,

        /// <summary>
        /// 
        /// </summary>
        Send3Option = 230,

        /// <summary>
        /// 
        /// </summary>
        Send4Option = 233,

        /// <summary>
        /// 
        /// </summary>
        Send5Option = 234,

        /// <summary>
        /// 
        /// </summary>
        Send2Options = 236,

        /// <summary>
        /// 
        /// </summary>
        Send4Options = 237,

        /// <summary>
        /// 
        /// </summary>
        Send5Options = 238,

        /// <summary>
        /// 
        /// </summary>
        Send2LargeOptions = 229,

        /// <summary>
        /// 
        /// </summary>
        Send3LargeOptions = 231,

        /// <summary>
        /// 
        /// </summary>
        Send1TextChatLeft = 241,

        /// <summary>
        /// 
        /// </summary>
        Send2TextChatLeft = 242,

        /// <summary>
        /// 
        /// </summary>
        Send3TextChatLeft = 243,

        /// <summary>
        /// 
        /// </summary>
        Send4TextChatLeft = 244,

        /// <summary>
        /// 
        /// </summary>
        SendNoContinue1TextChat = 245,

        /// <summary>
        /// 
        /// </summary>
        SendNoContinue2TextChat = 246,

        /// <summary>
        /// 
        /// </summary>
        SendNoContinue3TextChat = 247,

        /// <summary>
        /// 
        /// </summary>
        SendNoContinue4TextChat = 248,

        /// <summary>
        /// 
        /// </summary>
        FoundClanDialog = 1094,

        /// <summary>
        /// 
        /// </summary>
        ClanMottoDialog = 1103,
    }

    /// <summary>
    /// Contains dialogue interface animation ids.
    /// </summary>
    public enum DialogueAnimations : short
    {
        NoAnimation = -1,
        CalmTalk = 9760,
        Crying = 9765,
        Worried = 9770,
        Sad = 9775,
        Scared = 9780,
        Mad = 9785,
        Angry = 9790,
        CrazyEye = 9795,
        CrazyEyeSecondary = 9800,
        NothingToSay = 9805,
        Staring = 9810,
        Yeah = 9815,
        Disgusted = 9820,
        NoWay = 9823,
        Confused = 9827,
        Drunk = 9835,
        Laughing = 9840,
        HeadSway = 9845,
        StiffFace = 9855,
        StiffFaceSecondary = 9860,
        Pridefull = 9865,
        Demented = 9870
    }

    /// <summary>
    /// Contains skill dialogue option ids.
    /// </summary>
    public enum InteractiveDialogueOptions : byte
    {
        /// <summary>
        /// 
        /// </summary>
        Make = 0,

        /// <summary>
        /// 
        /// </summary>
        MakeSets = 1,

        /// <summary>
        /// 
        /// </summary>
        Cook = 2,

        /// <summary>
        /// 
        /// </summary>
        Roast = 3,

        /// <summary>
        /// 
        /// </summary>
        Offer = 4,

        /// <summary>
        /// 
        /// </summary>
        Sell = 5,

        /// <summary>
        /// 
        /// </summary>
        Bake = 6,

        /// <summary>
        /// 
        /// </summary>
        Cut = 7,

        /// <summary>
        /// 
        /// </summary>
        Deposit = 8,

        /// <summary>
        /// 
        /// </summary>
        MakeNoAllNoCustom = 9,

        /// <summary>
        /// 
        /// </summary>
        Teleport = 10,

        /// <summary>
        /// 
        /// </summary>
        Select = 11,

        /// <summary>
        /// 
        /// </summary>
        Take = 13
    }
}