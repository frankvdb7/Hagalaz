using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.Clans;
using Hagalaz.Game.Abstractions.Features.Shops;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Model;
using Hagalaz.Services.GameWorld.Model.Maps;
using Hagalaz.Services.GameWorld.Providers;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Represents a 'character' entity.
    /// </summary>
    public partial class Character : Creature, ICharacter
    {
        /// <summary>
        /// Contains character scripts.
        /// </summary>
        private readonly Dictionary<Type, ICharacterScript> _scripts = default!;

        /// <summary>
        /// Contains character option handlers.
        /// </summary>
        private readonly CharacterOptionClicked?[] _optionHandlers = new CharacterOptionClicked?[10];

        /// <summary>
        /// Contains the displayed hint icons.
        /// </summary>
        private readonly IHintIcon?[] _hintIcons = new IHintIcon[8];

        /// <summary>
        /// The path finder
        /// </summary>
        private readonly IPathFinder _pathFinder;

        /// <summary>
        /// The event manager
        /// </summary>
        public IEventManager EventManager { get; }

        /// <summary>
        /// A permanent id given uniquely to this user 
        /// to define the master id for the database.
        /// </summary>
        public uint MasterId => Session.MasterId;

        /// <summary>
        /// Contains all special permissions that this character has been given.
        /// </summary>
        public Permission Permissions { get; private set; }

        /// <summary>
        /// The character's session.
        /// </summary>
        public IGameSession Session { get; }

        public override int Size => Appearance.Size;

        /// <summary>
        /// Whether the character is muted.
        /// </summary>
        public bool IsMuted { get; private set; }

        /// <summary>
        /// Contains the last game login.
        /// </summary>
        public DateTimeOffset LastLogin { get; private set; }

        /// <summary>
        /// Gets a value indicating whether [received welcome].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [received welcome]; otherwise, <c>false</c>.
        /// </value>
        public bool HasReceivedWelcome { get; set; }

        /// <summary>
        /// Contains character game client.
        /// </summary>
        public IGameClient GameClient { get; private set; }

        /// <summary>
        /// The character's GPI data.
        /// </summary>
        public ICharacterRenderInformation RenderInformation { get; private set; }

        /// <summary>
        /// Contains character interfaces.
        /// </summary>
        public IWidgetContainer Widgets { get; private set; }

        /// <summary>
        /// Contains the character friend list.
        /// </summary>
        public IContactList<Friend> Friends { get; set; } = new ContactList<Friend>();

        /// <summary>
        /// Contains the character ignore list.
        /// </summary>
        public IContactList<Ignore> Ignores { get; set; } = new ContactList<Ignore>();

        /// <summary>
        /// Contains character minimap dot.
        /// </summary>
        public MiniMapDot MiniMapDot { get; private set; }

        /// <summary>
        /// The character's inventory container.
        /// </summary>
        public IInventoryContainer Inventory { get; private set; }

        /// <summary>
        /// The character's equipment container.
        /// </summary>
        public IEquipmentContainer Equipment { get; private set; }

        /// <summary>
        /// The character' bank container.
        /// </summary>
        public IBankContainer Bank { get; private set; }

        /// <summary>
        /// The character's reward container.
        /// </summary>
        public IRewardContainer Rewards { get; private set; }

        /// <summary>
        /// The character's money pouch container.
        /// </summary>
        public IMoneyPouchContainer MoneyPouch { get; private set; }

        /// <summary>
        /// Contains character configurations.
        /// </summary>
        public IConfigurations Configurations { get; private set; }

        /// <summary>
        /// Contains character statistics.
        /// </summary>
        public ICharacterStatistics Statistics { get; private set; }

        /// <summary>
        /// Contains character appearance.
        /// </summary>
        public ICharacterAppearance Appearance { get; private set; }

        /// <summary>
        /// Contains character prayers.
        /// </summary>
        public IPrayers Prayers { get; private set; }

        /// <summary>
        /// Contains character profile
        /// </summary>
        public IProfile Profile { get; private set; }

        /// <summary>
        /// Contains the shop the character is viewing.
        /// </summary>
        public IShop? CurrentShop { get; set; }

        /// <summary>
        /// Contains the familiar script, if any.
        /// </summary>
        /// <value>The familiar script.</value>
        public IFamiliarScript FamiliarScript { get; set; }

        /// <summary>
        /// Contains the previous display name.
        /// </summary>
        public string? PreviousDisplayName { get; set; }

        /// <summary>
        /// Contains the quests of the character.
        /// </summary>
        public Quests Quests { get; private set; }

        /// <summary>
        /// Contains the slayer of the character.
        /// </summary>
        public ISlayer Slayer { get; private set; }

        /// <summary>
        /// Contains the farming of the character.
        /// </summary>
        public IFarming Farming { get; private set; }

        /// <summary>
        /// Contains the magic of the character.
        /// </summary>
        public IMagic Magic { get; private set; }

        /// <summary>
        /// Contains the music settings for the character.
        /// </summary>
        public IMusic Music { get; private set; }

        /// <summary>
        /// Contains the notes of the character.
        /// </summary>
        public INotes Notes { get; private set; }

        /// <summary>
        /// Gets or sets the name of the clan.
        /// </summary>
        /// <value>
        /// The name of the clan.
        /// </value>
        public IClan Clan { get; set; }

        /// <summary>
        /// Gets the path finder.
        /// </summary>
        /// <value>
        /// The path finder.
        /// </value>
        public override IPathFinder PathFinder => _pathFinder;

        /// <summary>
        /// Gets the type of the current chat.
        /// </summary>
        /// <value>
        /// The type of the current chat.
        /// </value>
        public ClientChatType CurrentChatType { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Character(IServiceScope serviceScope, IGameSession session, IGameClient gameClient) : base(serviceScope)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            GameClient = gameClient;
            Session = session;
            var contextProvider = serviceScope.ServiceProvider.GetRequiredService<ICharacterContextProvider>();
            contextProvider.Context = new CharacterContext(this);
            EventManager = ServiceProvider.GetRequiredService<IEventManager>();
            _pathFinder = ServiceProvider.GetRequiredService<ISmartPathFinder>();
            Configurations = new Configurations(this);
            Widgets = new WidgetContainer(this);

            Viewport = new Viewport(this, MapRegionService, MapSize.Default);
            Movement = new Movement(this);

            // Updating core.
            RenderInformation = new CharacterRenderInformation(this);

            Inventory = new InventoryContainer(this, 28);
            Equipment = new EquipmentContainer(this, 15);
            Bank = new BankContainer(this, 500);
            Rewards = new RewardContainer(this);
            MoneyPouch = new MoneyPouchContainer(this);
            Statistics = new CharacterStatistics(this);
            Appearance = new CharacterAppearance(this);
            Prayers = new Prayers(this);
            Combat = new CharacterCombat(this);
            Quests = new Quests(this);
            Farming = new Farming(this);
            Slayer = new Slayer(this);
            Music = new Music(this);
            Notes = new Notes(this);
            Magic = new Magic(this);
            Profile = new Profile();

            // load scripts.
            var scripts = ServiceProvider.GetRequiredService<IDefaultCharacterScriptProvider>().GetAllScripts().Cast<ICharacterScript>().ToList();
            _scripts = scripts.ToDictionary(s => s.GetType(), s => s);
        }

        /// <summary>
        /// Character's cannot be destroyed,
        /// they must be unlinked manually.
        /// </summary>
        /// <returns></returns>
        public override bool CanDestroy() => false;

        /// <summary>
        /// Character's cannot be suspended ,
        /// returns false.
        /// </summary>
        /// <returns></returns>
        public override bool CanSuspend() => false;

        /// <summary>
        /// Happens when character is destroyed.
        /// </summary>
        /// <returns></returns>
        protected override void OnDestroy()
        {
            EventManager.SendEvent(new CreatureDestroyedEvent(this));
            foreach (var characterScript in _scripts.Values)
            {
                characterScript.OnDestroy();
            }
            UnregisterEventHandlers();
        }

        /// <summary>
        /// Get's called when entity is registered to world.
        /// </summary>
        public override async Task OnRegistered()
        {
            // initialize the most important drawing logic first
            RenderInformation.OnRegistered();
            // also sends the 'start-up' packet aka map and character sync
            await UpdateMapAsync(true, true);

            // and then initialize the rest
            SetLocation(Location, true, true);

            RegisterEventHandlers();

            Appearance.DrawCharacter();
            Appearance.Refresh();

            foreach (var script in _scripts.Values)
                script.OnRegistered();

            foreach (var state in States.Values.ToList())
                state.Script.OnStateAdded(state, this);

            if (!HasState(StateType.LodestoneEdgeville))
                AddState(new State(StateType.LodestoneEdgeville, int.MaxValue));

            OnInit();
        }

        /// <summary>
        /// Tick's this character.
        /// </summary>
        private void Tick()
        {
            Prayers.Tick();
            Statistics.Tick();
            foreach (var characterScript in _scripts.Values)
            {
                characterScript.Tick();
            }

            if (Appearance.IsTransformedToNpc())
                Appearance.PnpcScript?.Tick();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"character[name={DisplayName},id={MasterId},index={Index},loc=({Location})]";
    }
}