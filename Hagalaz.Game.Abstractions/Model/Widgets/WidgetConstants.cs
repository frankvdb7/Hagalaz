namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// Defines the IDs for various common widgets (interfaces).
    /// </summary>
    public enum InterfaceIds
    {
        /// <summary>
        /// The character design interface.
        /// </summary>
        AccountDesignInterface = 1028,
        /// <summary>
        /// The main lobby frame.
        /// </summary>
        LobbyFrame = 906,
        /// <summary>
        /// The frame for the character design screen.
        /// </summary>
        AccountDesignFrame = 753,
        /// <summary>
        /// The main game frame for fixed-screen mode.
        /// </summary>
        FixedFrame = 548,
        /// <summary>
        /// The main game frame for resizable-screen mode.
        /// </summary>
        ResizedFrame = 746,
        /// <summary>
        /// The chatbox frame.
        /// </summary>
        ChatboxFrame = 752,
        /// <summary>
        /// The inventory widget.
        /// </summary>
        Inventory = 679,
        /// <summary>
        /// The friends chat settings widget.
        /// </summary>
        FriendsChatSettings = 1108
    }

    /// <summary>
    /// Defines the component slot IDs for various common interface locations.
    /// </summary>
    public enum InterfaceSlots
    {
        /// <summary>
        /// The inventory slot in fixed-screen mode.
        /// </summary>
        FixedInventorySlot = 183,
        /// <summary>
        /// The inventory slot in resizable-screen mode.
        /// </summary>
        ResizedInventorySlot = 158,
        /// <summary>
        /// The main interface slot in fixed-screen mode (where tabs like skills, quests, etc. appear).
        /// </summary>
        FixedMainInterfaceSlot = 47,
        /// <summary>
        /// The main interface slot in resizable-screen mode.
        /// </summary>
        ResizedMainInterfaceSlot = 70,
        /// <summary>
        /// The inventory overlay slot in fixed-screen mode.
        /// </summary>
        FixedInventoryOverlay = 175,
        /// <summary>
        /// The inventory overlay slot in resizable-screen mode.
        /// </summary>
        ResizedInventoryOverlay = 151,
        /// <summary>
        /// The chatbox overlay slot.
        /// </summary>
        ChatboxOverlay = 13,
    }
}