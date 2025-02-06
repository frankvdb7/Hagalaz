using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.Clans;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICharacter : ICreature
    {
        /// <summary>
        /// Gets the session.
        /// </summary>
        /// <value>
        /// The session.
        /// </value>
        IGameSession Session { get; }
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        new string Name { get; }
        /// <summary>
        /// Contains the last game login.
        /// </summary>
        DateTimeOffset LastLogin { get; }
        /// <summary>
        /// Gets a value indicating whether [received welcome].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [received welcome]; otherwise, <c>false</c>.
        /// </value>
        bool HasReceivedWelcome { get; }
        int QueuedAnimationsCount { get; }
        int QueuedGraphicsCount { get; }
        IAnimation TakeAnimation();
        IGraphic TakeGraphic();
        IEnumerable<ICharacterScript> GetScripts();
        bool RenderSkull(SkullIcon icon, int ticks);
        void OnSkillExperienceGain(int skillID, double currentExperience);
        /// <summary>
        /// Gets the master identifier.
        /// </summary>
        /// <value>
        /// The master identifier.
        /// </value>
        uint MasterId {get; }
        /// <summary>
        /// Contains the previous display name.
        /// </summary>
        string? PreviousDisplayName { get; set; }
        /// <summary>
        /// Gets the familiar script.
        /// </summary>
        /// <value>
        /// The familiar script.
        /// </value>
        IFamiliarScript FamiliarScript { get; }
        /// <summary>
        /// Contains all special permissions that this character has been given.
        /// </summary>
        Permission Permissions { get; }
        /// <summary>
        /// Gets the statistics.
        /// </summary>
        /// <value>
        /// The statistics.
        /// </value>
        ICharacterStatistics Statistics { get; }
        /// <summary>
        /// Contains the slayer of the character.
        /// </summary>
        ISlayer Slayer { get; }
        /// <summary>
        /// Contains the farming of the character.
        /// </summary>
        IFarming Farming { get; }
        /// <summary>
        /// Contains the magic of the character.
        /// </summary>
        IMagic Magic { get; }
        /// <summary>
        /// Gets the notes.
        /// </summary>
        /// <value>
        /// The notes.
        /// </value>
        INotes Notes { get; }
        /// <summary>
        /// Gets the appearance.
        /// </summary>
        /// <value>
        /// The appearance.
        /// </value>
        ICharacterAppearance Appearance { get; }
        /// <summary>
        /// Gets the render information.
        /// </summary>
        /// <value>
        /// The render information.
        /// </value>
        ICharacterRenderInformation RenderInformation { get; }
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        IConfigurations Configurations { get; }
        /// <summary>
        /// Gets the prayers.
        /// </summary>
        /// <value>
        /// The prayers.
        /// </value>
        IPrayers Prayers { get; }
        /// <summary>
        /// Gets or sets the clan.
        /// </summary>
        /// <value>
        /// The clan.
        /// </value>
        IClan Clan { get; set; }
        /// <summary>
        /// Gets the friends list
        /// </summary>
        IContactList<Friend> Friends { get; }
        /// <summary>
        /// Gets the ignore list
        /// </summary>
        IContactList<Ignore> Ignores { get; }
        /// <summary>
        /// Gets the music.
        /// </summary>
        /// <value>
        /// The music.
        /// </value>
        IMusic Music { get; }
        /// <summary>
        /// Gets the interfaces.
        /// </summary>
        /// <value>
        /// The interfaces.
        /// </value>
        IWidgetContainer Widgets { get; }
        /// <summary>
        /// Gets the game client.
        /// </summary>
        /// <value>
        /// The game client.
        /// </value>
        IGameClient GameClient { get; }
        /// <summary>
        /// Gets the profile.
        /// </summary>
        IProfile Profile { get; }
        /// <summary>
        /// Gets the inventory.
        /// </summary>
        /// <value>
        /// The inventory.
        /// </value>
        IInventoryContainer Inventory { get; }
        /// <summary>
        /// Gets the equipment.
        /// </summary>
        /// <value>
        /// The equipment.
        /// </value>
        IEquipmentContainer Equipment { get; }
        /// <summary>
        /// Gets the money pouch.
        /// </summary>
        /// <value>
        /// The money pouch.
        /// </value>
        IMoneyPouchContainer MoneyPouch { get; }
        /// <summary>
        /// Gets the rewards.
        /// </summary>
        /// <value>
        /// The rewards.
        /// </value>
        IRewardContainer Rewards { get; }
        /// <summary>
        /// Gets the bank.
        /// </summary>
        /// <value>
        /// The bank.
        /// </value>
        IBankContainer Bank { get; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="ICharacter"/> is muted.
        /// </summary>
        /// <value>
        ///   <c>true</c> if muted; otherwise, <c>false</c>.
        /// </value>
        bool IsMuted { get; }
        /// <summary>
        /// Gets the type of the current chat.
        /// </summary>
        /// <value>
        /// The type of the current chat.
        /// </value>
        ClientChatType CurrentChatType { get; set; }
        /// <summary>
        /// Gets or sets the current shop.
        /// </summary>
        /// <value>
        /// The current shop.
        /// </value>
        IShop? CurrentShop { get; set; }
        /// <summary>
        /// Get's if character is busy.
        /// </summary>
        /// <returns><c>true</c> if this instance is busy; otherwise, <c>false</c>.</returns>
        bool IsBusy();
        /// <summary>
        /// Interrupts the interfaces.
        /// </summary>
        void InterruptInterfaces();
        /// <summary>
        /// Update's character visible regions.
        /// </summary>
        Task UpdateMapAsync(bool forceUpdate, bool renderViewPort = false);

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="messageType">Type of the message.</param>
        /// <param name="displayName"></param>
        /// <param name="previousDisplayName"></param>
        void SendChatMessage(string message, ChatMessageType messageType = ChatMessageType.ChatboxText, string? displayName = null, string? previousDisplayName = null);

        /// <summary>
        /// Registers the hint icon.
        /// </summary>
        /// <param name="icon">The icon.</param>
        /// <returns></returns>
        bool TryRegisterHintIcon(IHintIcon icon);

        /// <summary>
        /// Tries to logout character.
        /// </summary>
        /// <param name="toLobby">Wheter character is logging out to lobby.</param>
        /// <returns>Returns true if character was logged out.</returns>
        bool TryLogout(bool toLobby);

        /// <summary>
        /// Unregisters the hint ion.
        /// </summary>
        /// <param name="icon">The icon.</param>
        /// <returns></returns>
        bool TryUnregisterHintIcon(IHintIcon icon);
        /// <summary>
        /// Gets the items get on death.
        /// Item1 = protected item slots
        /// Item2 = destroyed item slots
        /// Item3 = dropped item slots
        /// Item4 = kept item slots
        /// </summary>
        /// <returns></returns>
        (List<int> protectedItems, List<int> destroyedItems, List<int> droppedItems, List<int> keptItems) GetItemSlotsOnDeathData();
        /// <summary>
        /// Gets the items on death data.
        /// Item1 = dropped items
        /// Item2 = kept items
        /// </summary>
        /// <returns></returns>
        (IItem[] droppedItems, IItem[] keptItems) GetItemsOnDeathData();
        /// <summary>
        /// Gets the items on death data.
        /// Item1 = dropped items
        /// Item2 = kept items
        /// </summary>
        /// <param name="slotData">The slot data.</param>
        /// <returns></returns>
        (IItem[] droppedItems, IItem[] keptItems) GetItemsOnDeathData((List<int> protectedItems, List<int> destroyedItems, List<int> droppedItems, List<int> keptItems) slotData);
        /// <summary>
        /// Happens when character option click packet is received.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="forceRun">Wheter character should force RUN.</param>
        /// <param name="target">Character that was clicked.</param>
        void OnCharacterClicked(CharacterClickType clickType, bool forceRun, ICharacter target);
        /// <summary>
        /// Trie's force logout on disconnection.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool TryForceLogout();
        /// <summary>
        /// Loads the script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script type.</typeparam>
        /// <returns>
        /// Null if there was already a script with the same type.
        /// </returns>
        /// <exception cref="System.Exception">Tried to add a non-initialized CharacterScript.</exception>
        TScriptType AddScript<TScriptType>() where TScriptType : class, ICharacterScript;
        /// <summary>
        /// Loads the script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script.</typeparam>
        /// <param name="script">The script.</param>
        /// <returns>
        /// Null if there was already a script with the same type.
        /// </returns>
        /// <exception cref="System.Exception">Tried to add a non-initialized CharacterScript.</exception>
        TScriptType AddScript<TScriptType>(TScriptType script) where TScriptType : class, ICharacterScript;
        /// <summary>
        /// Determines whether this instance has script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the cript type.</typeparam>
        /// <returns>
        ///   <c>true</c> if this instance has script; otherwise, <c>false</c>.
        /// </returns>
        bool HasScript<TScriptType>() where TScriptType : class, ICharacterScript;
        /// <summary>
        /// Gets the script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the cript type.</typeparam>
        /// <returns></returns>
        TScriptType? GetScript<TScriptType>() where TScriptType : class, ICharacterScript;
        /// <summary>
        /// Removes the script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script.</typeparam>
        /// <returns>If the script was successfully removed.</returns>
        bool TryRemoveScript<TScriptType>() where TScriptType : class, ICharacterScript;
        /// <summary>
        /// Registers the characters option handler.
        /// </summary>
        /// <param name="clickType"></param>
        /// <param name="optionName">DisplayName of the option.</param>
        /// <param name="iconID">The icon Id.</param>
        /// <param name="showOnTop">if set to <c>true</c> [show on top].</param>
        /// <param name="handler">The handler.</param>
        /// <exception cref="System.Exception"></exception>
        void RegisterCharactersOptionHandler(CharacterClickType clickType, string optionName, int iconID, bool showOnTop, CharacterOptionClicked handler);

        /// <summary>
        /// Unregisters the characters option handler.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        void UnregisterCharactersOptionHandler(CharacterClickType clickType);
    }
}
