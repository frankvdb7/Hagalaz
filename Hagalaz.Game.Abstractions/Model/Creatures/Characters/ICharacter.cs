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
    /// Defines the contract for a player-controlled character, extending the base creature with player-specific properties and behaviors.
    /// </summary>
    public interface ICharacter : ICreature
    {
        /// <summary>
        /// Gets the game session associated with this character.
        /// </summary>
        IGameSession Session { get; }
        /// <summary>
        /// Gets the unique display name of the character.
        /// </summary>
        new string Name { get; }
        /// <summary>
        /// Gets the timestamp of the character's last login.
        /// </summary>
        DateTimeOffset LastLogin { get; }
        /// <summary>
        /// Gets a value indicating whether the character has received the initial welcome message for this session.
        /// </summary>
        bool HasReceivedWelcome { get; }
        /// <summary>
        /// Gets the number of animations currently queued for this character.
        /// </summary>
        int QueuedAnimationsCount { get; }
        /// <summary>
        /// Gets the number of graphics currently queued for this character.
        /// </summary>
        int QueuedGraphicsCount { get; }
        /// <summary>
        /// Dequeues and returns the next animation in the queue.
        /// </summary>
        /// <returns>The next <see cref="IAnimation"/> to be played.</returns>
        IAnimation TakeAnimation();
        /// <summary>
        /// Dequeues and returns the next graphic in the queue.
        /// </summary>
        /// <returns>The next <see cref="IGraphic"/> to be displayed.</returns>
        IGraphic TakeGraphic();
        /// <summary>
        /// Gets an enumeration of all scripts currently attached to this character.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="ICharacterScript"/>.</returns>
        IEnumerable<ICharacterScript> GetScripts();
        /// <summary>
        /// Renders a skull icon above the character's head, typically indicating a PvP status.
        /// </summary>
        /// <param name="icon">The type of skull icon to display.</param>
        /// <param name="ticks">The duration in game ticks for which the skull will be visible.</param>
        /// <returns><c>true</c> if the skull was successfully rendered; otherwise, <c>false</c>.</returns>
        bool RenderSkull(SkullIcon icon, int ticks);
        /// <summary>
        /// A callback method executed when the character gains experience in a skill.
        /// </summary>
        /// <param name="skillID">The ID of the skill that gained experience.</param>
        /// <param name="currentExperience">The total current experience in the skill after the gain.</param>
        void OnSkillExperienceGain(int skillID, double currentExperience);
        /// <summary>
        /// Gets the unique identifier for the player's master account.
        /// </summary>
        uint MasterId {get; }
        /// <summary>
        /// Gets or sets the character's previous display name, used for tracking name changes.
        /// </summary>
        string? PreviousDisplayName { get; set; }
        /// <summary>
        /// Gets the script for the character's currently summoned familiar.
        /// </summary>
        IFamiliarScript FamiliarScript { get; }
        /// <summary>
        /// Gets the special permissions and rights assigned to this character.
        /// </summary>
        Permission Permissions { get; }
        /// <summary>
        /// Gets the handler for the character's statistics, including skills and life points.
        /// </summary>
        ICharacterStatistics Statistics { get; }
        /// <summary>
        /// Gets the handler for the character's Slayer skill data and tasks.
        /// </summary>
        ISlayer Slayer { get; }
        /// <summary>
        /// Gets the handler for the character's Farming skill data and patches.
        /// </summary>
        IFarming Farming { get; }
        /// <summary>
        /// Gets the handler for the character's Magic skill, including spellbook and runes.
        /// </summary>
        IMagic Magic { get; }
        /// <summary>
        /// Gets the handler for the character's in-game notes.
        /// </summary>
        INotes Notes { get; }
        /// <summary>
        /// Gets the handler for the character's visual appearance and customization.
        /// </summary>
        ICharacterAppearance Appearance { get; }
        /// <summary>
        /// Gets the handler for the character's client-side rendering information.
        /// </summary>
        ICharacterRenderInformation RenderInformation { get; }
        /// <summary>
        /// Gets the handler for the character's various settings and configurations.
        /// </summary>
        IConfigurations Configurations { get; }
        /// <summary>
        /// Gets the handler for the character's prayers and curses.
        /// </summary>
        IPrayers Prayers { get; }
        /// <summary>
        /// Gets or sets the clan that the character is a member of.
        /// </summary>
        IClan Clan { get; set; }
        /// <summary>
        /// Gets the character's friends list.
        /// </summary>
        IContactList<Friend> Friends { get; }
        /// <summary>
        /// Gets the character's ignore list.
        /// </summary>
        IContactList<Ignore> Ignores { get; }
        /// <summary>
        /// Gets the handler for the character's unlocked music tracks.
        /// </summary>
        IMusic Music { get; }
        /// <summary>
        /// Gets the container for the character's user interface widgets (interfaces).
        /// </summary>
        IWidgetContainer Widgets { get; }
        /// <summary>
        /// Gets the handler for the character's game client settings and state.
        /// </summary>
        IGameClient GameClient { get; }
        /// <summary>
        /// Gets the handler for the character's persistent profile data.
        /// </summary>
        IProfile Profile { get; }
        /// <summary>
        /// Gets the character's inventory container.
        /// </summary>
        IInventoryContainer Inventory { get; }
        /// <summary>
        /// Gets the character's equipment container.
        /// </summary>
        IEquipmentContainer Equipment { get; }
        /// <summary>
        /// Gets the character's money pouch container.
        /// </summary>
        IMoneyPouchContainer MoneyPouch { get; }
        /// <summary>
        /// Gets the character's container for unclaimed rewards.
        /// </summary>
        IRewardContainer Rewards { get; }
        /// <summary>
        /// Gets the character's bank container.
        /// </summary>
        IBankContainer Bank { get; }
        /// <summary>
        /// Gets a value indicating whether the character is currently muted and unable to use chat.
        /// </summary>
        bool IsMuted { get; }
        /// <summary>
        /// Gets or sets the character's current chat channel (e.g., public, friends, clan).
        /// </summary>
        ClientChatType CurrentChatType { get; set; }
        /// <summary>
        /// Gets or sets the shop that the character is currently interacting with.
        /// </summary>
        IShop? CurrentShop { get; set; }
        /// <summary>
        /// Determines if the character is currently busy with an action that prevents other interactions (e.g., in a cutscene, trading).
        /// </summary>
        /// <returns><c>true</c> if the character is busy; otherwise, <c>false</c>.</returns>
        bool IsBusy();
        /// <summary>
        /// Closes any open user interface widgets.
        /// </summary>
        void InterruptInterfaces();
        /// <summary>
        /// Updates the character's client with the latest map and entity information for their visible area.
        /// </summary>
        /// <param name="forceUpdate">If set to <c>true</c>, forces a full map rebuild.</param>
        /// <param name="renderViewPort">If set to <c>true</c>, forces the viewport to be re-rendered.</param>
        Task UpdateMapAsync(bool forceUpdate, bool renderViewPort = false);
        /// <summary>
        /// Sends a message to the character's chatbox or console.
        /// </summary>
        /// <param name="message">The message content.</param>
        /// <param name="messageType">The type of message, which determines its color and formatting.</param>
        /// <param name="displayName">The display name of the sender, if applicable.</param>
        /// <param name="previousDisplayName">The previous display name of the sender, if applicable (for name changes).</param>
        void SendChatMessage(string message, ChatMessageType messageType = ChatMessageType.ChatboxText, string? displayName = null, string? previousDisplayName = null);
        /// <summary>
        /// Attempts to register and display a hint icon for the character.
        /// </summary>
        /// <param name="icon">The hint icon to display.</param>
        /// <returns><c>true</c> if the hint icon was successfully registered; otherwise, <c>false</c>.</returns>
        bool TryRegisterHintIcon(IHintIcon icon);
        /// <summary>
        /// Attempts to log the character out of the game.
        /// </summary>
        /// <param name="toLobby">If set to <c>true</c>, the character will be sent to the lobby instead of being fully disconnected.</param>
        /// <returns><c>true</c> if the logout was successful; otherwise, <c>false</c>.</returns>
        bool TryLogout(bool toLobby);
        /// <summary>
        /// Attempts to unregister and remove an active hint icon.
        /// </summary>
        /// <param name="icon">The hint icon to remove.</param>
        /// <returns><c>true</c> if the hint icon was successfully unregistered; otherwise, <c>false</c>.</returns>
        bool TryUnregisterHintIcon(IHintIcon icon);
        /// <summary>
        /// Calculates which item slots are protected, destroyed, dropped, and kept upon death.
        /// </summary>
        /// <returns>A tuple containing lists of item slot indices for each category.</returns>
        (List<int> protectedItems, List<int> destroyedItems, List<int> droppedItems, List<int> keptItems) GetItemSlotsOnDeathData();
        /// <summary>
        /// Gets the actual items that will be dropped and kept upon the character's death.
        /// </summary>
        /// <returns>A tuple containing an array of dropped items and an array of kept items.</returns>
        (IItem[] droppedItems, IItem[] keptItems) GetItemsOnDeathData();
        /// <summary>
        /// Gets the items that will be dropped and kept upon death, based on pre-calculated slot data.
        /// </summary>
        /// <param name="slotData">The pre-calculated data of which slots are protected, destroyed, etc.</param>
        /// <returns>A tuple containing an array of dropped items and an array of kept items.</returns>
        (IItem[] droppedItems, IItem[] keptItems) GetItemsOnDeathData((List<int> protectedItems, List<int> destroyedItems, List<int> droppedItems, List<int> keptItems) slotData);
        /// <summary>
        /// A callback method executed when this character clicks on another character.
        /// </summary>
        /// <param name="clickType">The type of click option selected.</param>
        /// <param name="forceRun">A value indicating whether the character should force-run to the target.</param>
        /// <param name="target">The character that was clicked on.</param>
        void OnCharacterClicked(CharacterClickType clickType, bool forceRun, ICharacter target);
        /// <summary>
        /// Attempts to forcefully log the character out, typically used on disconnection.
        /// </summary>
        /// <returns><c>true</c> if the force logout was successful; otherwise, <c>false</c>.</returns>
        bool TryForceLogout();
        /// <summary>
        /// Adds a new script component of a specified type to the character.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script to add.</typeparam>
        /// <returns>The newly created and attached script instance.</returns>
        TScriptType AddScript<TScriptType>() where TScriptType : class, ICharacterScript;
        /// <summary>
        /// Adds an existing script component instance to the character.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script.</typeparam>
        /// <param name="script">The script instance to add.</param>
        /// <returns>The attached script instance.</returns>
        TScriptType AddScript<TScriptType>(TScriptType script) where TScriptType : class, ICharacterScript;
        /// <summary>
        /// Checks if the character has a script of a specific type attached.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script to check for.</typeparam>
        /// <returns><c>true</c> if a script of the specified type is attached; otherwise, <c>false</c>.</returns>
        bool HasScript<TScriptType>() where TScriptType : class, ICharacterScript;
        /// <summary>
        /// Retrieves a script of a specific type that is attached to the character.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script to retrieve.</typeparam>
        /// <returns>The script instance if found; otherwise, <c>null</c>.</returns>
        TScriptType? GetScript<TScriptType>() where TScriptType : class, ICharacterScript;
        /// <summary>
        /// Attempts to remove a script of a specific type from the character.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script to remove.</typeparam>
        /// <returns><c>true</c> if the script was successfully removed; otherwise, <c>false</c>.</returns>
        bool TryRemoveScript<TScriptType>() where TScriptType : class, ICharacterScript;
        /// <summary>
        /// Registers a custom right-click option on this character that other players can interact with.
        /// </summary>
        /// <param name="clickType">The click option slot to register.</param>
        /// <param name="optionName">The text to be displayed for the option.</param>
        /// <param name="iconID">The ID of the icon to display next to the option text.</param>
        /// <param name="showOnTop">If set to <c>true</c>, this option will appear at the top of the context menu.</param>
        /// <param name="handler">The delegate to be executed when the option is clicked.</param>
        void RegisterCharactersOptionHandler(CharacterClickType clickType, string optionName, int iconID, bool showOnTop, CharacterOptionClicked handler);
        /// <summary>
        /// Unregisters a previously registered custom right-click option.
        /// </summary>
        /// <param name="clickType">The click option slot to unregister.</param>
        void UnregisterCharactersOptionHandler(CharacterClickType clickType);
    }
}