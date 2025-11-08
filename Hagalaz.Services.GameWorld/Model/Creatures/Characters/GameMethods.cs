using System.Collections.Generic;
using System.Linq;
using Hagalaz.Cache.Abstractions.Model;
using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Features.States;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Model;
using Microsoft.Extensions.DependencyInjection;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Contains game methods.
    /// </summary>
    public partial class Character : Creature
    {
        /// <summary>
        /// Registers the characters option handler.
        /// </summary>
        /// <param name="clickType"></param>
        /// <param name="optionName">DisplayName of the option.</param>
        /// <param name="iconID">The icon Id.</param>
        /// <param name="showOnTop">if set to <c>true</c> [show on top].</param>
        /// <param name="handler">The handler.</param>
        public void RegisterCharactersOptionHandler(
            CharacterClickType clickType,
            string optionName,
            int iconID,
            bool showOnTop,
            CharacterOptionClicked handler)
        {
            _optionHandlers[(int)clickType - 1] = handler;
            Session.SendMessage(new SetCharacterOptionMessage
            {
                Type = clickType, Name = optionName, IconId = iconID, DrawOnTop = showOnTop,
            });
        }

        /// <summary>
        /// Unregisters the characters option handler.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public void UnregisterCharactersOptionHandler(CharacterClickType clickType)
        {
            if (_optionHandlers[(int)clickType - 1] == null)
            {
                return;
            }

            _optionHandlers[(int)clickType - 1] = null;
            Session.SendMessage(new SetCharacterOptionMessage
            {
                Type = clickType, Name = "null", IconId = -1, DrawOnTop = false
            });
        }

        /// <summary>
        /// Registers the hint icon.
        /// </summary>
        /// <param name="icon">The icon.</param>
        /// <returns></returns>
        public bool TryRegisterHintIcon(IHintIcon icon)
        {
            for (var index = 0; index < _hintIcons.Length; index++)
            {
                if (_hintIcons[index] == null)
                {
                    _hintIcons[index] = icon;
                    icon.Index = index;

                    Session.SendMessage(icon.ToMessage());
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Unregisters the hint ion.
        /// </summary>
        /// <param name="icon">The icon.</param>
        /// <returns></returns>
        public bool TryUnregisterHintIcon(IHintIcon icon)
        {
            if (icon.Index < 0 || icon.Index > _hintIcons.Length)
            {
                return false;
            }

            if (_hintIcons[icon.Index] != icon)
            {
                return false;
            }

            _hintIcons[icon.Index] = null;
            Session.SendMessage(new RemoveHintIconMessage
            {
                IconIndex = icon.Index
            });
            return true;
        }

        /// <summary>
        /// Tries to logout character.
        /// </summary>
        /// <param name="toLobby">Wheter character is logging out to lobby.</param>
        /// <returns>Returns true if character was logged out.</returns>
        public bool TryLogout(bool toLobby)
        {
            if (Combat.IsInCombat())
            {
                SendChatMessage("You can't logout in combat and 10 seconds after it.");
                return false;
            }

            if (!EventManager.SendEvent(new LogoutAllowEvent(this)))
            {
                return false;
            }

            Session.SendMessage(toLobby ? new LogoutToLobbyMessage() : new LogoutToLoginMessage());
            return true;
        }

        /// <summary>
        /// Trie's force logout on disconnection.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool TryForceLogout() => !Combat.IsInCombat() && EventManager.SendEvent(new LogoutAllowEvent(this));

        /// <summary>
        /// Get's called when character dies.
        /// </summary>
        public override void OnDeath()
        {
            if (Combat.IsDead)
            {
                return;
            }

            var soundBuilder = ServiceProvider.GetRequiredService<IAudioBuilder>();

            var sound = soundBuilder.Create().AsMusicEffect().WithId(90).Build(); // death music effect
            Session.SendMessage(sound.ToMessage());
            EventManager.SendEvent(new CreatureDiedEvent(this));
            Movement.Lock(true); // reset the movement and lock the character
            foreach (var item in Equipment)
            {
                item?.EquipmentScript.OnDeath(item, this);
            }

            foreach (var script in _scripts.Values) script.OnDeath();
            if (Appearance.IsTransformedToNpc()) Appearance.PnpcScript.OnDeath();
            Combat.OnDeath();
        }

        /// <summary>
        /// Get's called when character spawns.
        /// </summary>
        public override void OnSpawn()
        {
            EventManager.SendEvent(new CreatureSpawnedEvent(this));
            foreach (var script in _scripts.Values) script.OnSpawn();
            if (Appearance.IsTransformedToNpc()) Appearance.PnpcScript.OnSpawn();
            Combat.OnSpawn();
        }

        /// <summary>
        /// Notifies character that it's location have been changed.
        /// </summary>
        /// <param name="oldLocation">The old location.</param>
        protected override void OnLocationChange(ILocation? oldLocation) => EventManager.SendEvent(new CreatureLocationChangedEvent(this, oldLocation));

        /// <summary>
        /// Get's called when the target creature is killed.
        /// </summary>
        /// <param name="target">The target.</param>
        public override void OnTargetKilled(ICreature target)
        {
            EventManager.SendEvent(new CreatureKillEvent(this, target));
            foreach (var script in _scripts.Values) script.OnTargetKilled(target);
            if (Appearance.IsTransformedToNpc()) Appearance.PnpcScript.OnTargetKilled(target);
            Combat.OnTargetKilled(target);
        }

        /// <summary>
        /// Get's called when killed by a creature.
        /// </summary>
        /// <param name="creature">The creature.</param>
        public override void OnKilledBy(ICreature creature)
        {
            EventManager.SendEvent(new CreatureKillEvent(creature, this));
            foreach (var script in _scripts.Values) script.OnKilledBy(creature);
            if (Appearance.IsTransformedToNpc()) Appearance.PnpcScript.OnDeathBy(creature);
            Combat.OnKilledBy(creature);
        }

        /// <summary>
        /// Gets the items get on death.
        /// Item1 = protected item slots
        /// Item2 = destroyed item slots
        /// Item3 = dropped item slots
        /// Item4 = kept item slots
        /// </summary>
        /// <returns></returns>
        public (List<int> protectedItems, List<int> destroyedItems, List<int> droppedItems, List<int> keptItems) GetItemSlotsOnDeathData()
        {
            var protectedItems = new List<int>(); // automatically keep
            var destroyedItems = new List<int>();
            var droppedItems = new List<int>();
            var totalCapacity = Inventory.Capacity + Equipment.Capacity;
            for (var slot = 0; slot < totalCapacity; slot++)
            {
                var item = slot >= Equipment.Capacity ? Inventory[slot - Equipment.Capacity] : Equipment[(EquipmentSlot)slot];
                if (item == null) continue;
                var degradeType = item.ItemDefinition.DegradeType;
                if (degradeType == DegradeType.ProtectedItem && !Area.IsPvP)
                    protectedItems.Add(slot + 1);
                else if (degradeType == DegradeType.DestroyItem)
                    destroyedItems.Add(slot + 1);
                else
                    droppedItems.Add(slot + 1);
            }

            droppedItems.Sort(Comparer<int>.Create((x, y) =>
            {
                var ix = x >= Equipment.Capacity ? Inventory[x - Equipment.Capacity - 1] : Equipment[(EquipmentSlot)(x - 1)];
                if (ix == null)
                {
                    return 0;
                }

                var iy = y >= Equipment.Capacity ? Inventory[y - Equipment.Capacity - 1] : Equipment[(EquipmentSlot)(y - 1)];
                if (iy == null)
                {
                    return 0;
                }

                if (ix.ItemDefinition.TradeValue > iy.ItemDefinition.TradeValue) return -1;
                if (ix.ItemDefinition.TradeValue < iy.ItemDefinition.TradeValue) return 1;
                return 0;
            }));
            var amountOfKeptItems = HasState(StateType.DefaultSkulled) ? HasState(StateType.ProtectOneItem) ? 1 : 0 :
                HasState(StateType.ProtectOneItem) ? 4 : 3;

            if (droppedItems.Count < amountOfKeptItems) amountOfKeptItems = droppedItems.Count;

            var keptItems = new List<int>();
            for (var i = 0; i < amountOfKeptItems; i++)
            {
                var slot = droppedItems[0];
                if (droppedItems.Remove(slot))
                {
                    keptItems.Add(slot);
                }
            }

            return (protectedItems, destroyedItems, droppedItems, keptItems);
        }

        /// <summary>
        /// Gets the items on death data.
        /// Item1 = dropped items
        /// Item2 = kept items
        /// </summary>
        /// <returns></returns>
        public (IItem[] droppedItems, IItem[] keptItems) GetItemsOnDeathData() => GetItemsOnDeathData(GetItemSlotsOnDeathData());

        /// <summary>
        /// Gets the items on death data.
        /// Item1 = dropped items
        /// Item2 = kept items
        /// </summary>
        /// <param name="slotData">The slot data.</param>
        /// <returns></returns>
        public (IItem[] droppedItems, IItem[] keptItems) GetItemsOnDeathData(
            (List<int> protectedItems, List<int> destroyedItems, List<int> droppedItems, List<int> keptItems) slotData)
        {
            IItem? slotItemConverter(int s) => s >= Equipment.Capacity ? Inventory[s - Equipment.Capacity - 1] : Equipment[(EquipmentSlot)(s - 1)];
            List<IItem> droppedItems = slotData.droppedItems.Select(slotItemConverter).ToList()!;
            var protectedItems = slotData.protectedItems.Select(slotItemConverter).OfType<IItem>();
            var keptItems = slotData.keptItems.Select((s) =>
                {
                    var item = slotItemConverter(s);
                    if (item?.Count > 1)
                    {
                        droppedItems.Add(item.Clone(item.Count - 1));
                        return item.Clone(1);
                    }

                    return item;
                })
                .OfType<IItem>();
            return (droppedItems.ToArray(), keptItems.Union(protectedItems).ToArray());
        }

        /// <summary>
        /// Get's if character is busy.
        /// </summary>
        /// <returns><c>true</c> if this instance is busy; otherwise, <c>false</c>.</returns>
        public bool IsBusy()
        {
            if (_scripts.Values.Any(script => script.IsBusy()))
            {
                return true;
            }

            if (Combat.IsDead || Combat.IsInCombat()) return true;
            var currentFrame = Widgets.CurrentFrame;
            if (currentFrame == null) return true;
            if (currentFrame.GetChild(GameClient.IsScreenFixed
                    ? (int)InterfaceSlots.FixedMainInterfaceSlot
                    : (int)InterfaceSlots.ResizedMainInterfaceSlot) != null)
                return true;
            if (currentFrame.GetChild(GameClient.IsScreenFixed ? (int)InterfaceSlots.FixedInventoryOverlay : (int)InterfaceSlots.ResizedInventoryOverlay) !=
                null)
                return true;
            var chatbox = Widgets.GetOpenWidget((short)InterfaceIds.ChatboxFrame);
            if (chatbox == null) return true;
            return chatbox.GetChild((short)InterfaceSlots.ChatboxOverlay) != null;
        }

        /// <summary>
        /// Interrupts current jobs.
        /// For example:Closes current main interface.
        /// </summary>
        /// <param name="source">Object which performed the interruption,
        /// this parameter can be null , but it is not recomended to do so.
        /// Best use would be to set the invoker class instance ('this') or any other related object
        /// if invoker is static as source.</param>
        /// <returns>If something was interupted.</returns>
        public override void Interrupt(object source)
        {
            EventManager.SendEvent(new CreatureInterruptedEvent(this, source));
            if (source is not CreatureCombat) Combat.CancelTarget();
            foreach (var script in _scripts.Values) script.OnInterrupt(source);
            var currentFrame = Widgets.CurrentFrame;
            var child = currentFrame?.GetChild(GameClient.IsScreenFixed
                ? (int)InterfaceSlots.FixedMainInterfaceSlot
                : (int)InterfaceSlots.ResizedMainInterfaceSlot);
            if (child != null && child.CanInterrupt()) Widgets.CloseWidget(child);
            var chatbox = Widgets.GetOpenWidget((int)InterfaceIds.ChatboxFrame);
            var overlay = chatbox?.GetChild((int)InterfaceSlots.ChatboxOverlay);
            if (overlay != null && overlay.CanInterrupt()) Widgets.CloseWidget(overlay);
        }

        /// <summary>
        /// Interrupts open interfaces.
        /// </summary>
        public void InterruptInterfaces()
        {
            var currentFrame = Widgets.CurrentFrame;
            var child = currentFrame?.GetChild(GameClient.IsScreenFixed
                ? (int)InterfaceSlots.FixedMainInterfaceSlot
                : (int)InterfaceSlots.ResizedMainInterfaceSlot);
            if (child != null && child.CanInterrupt()) Widgets.CloseWidget(child);
            var chatbox = Widgets.GetOpenWidget((int)InterfaceIds.ChatboxFrame);
            var overlay = chatbox?.GetChild((int)InterfaceSlots.ChatboxOverlay);
            if (overlay != null && overlay.CanInterrupt()) Widgets.CloseWidget(overlay);
        }

        /// <summary>
        /// Respawn's character.
        /// </summary>
        public override void Respawn()
        {
            // remove states that should not remain on death.
            RemoveState(StateType.DefaultSkulled);
            RemoveState(StateType.ResistFreeze);
            RemoveState(StateType.ResistPoison);
            RemoveState(StateType.Stun);
            RemoveState(StateType.SuperAntiDragonfirePotion);
            RemoveState(StateType.TeleBlocked);
            RemoveState(StateType.Vengeance);
            RemoveState(StateType.Injured);
            RemoveState(StateType.Frozen);
            RemoveState(StateType.CantCastVengeance);

            Prayers.DeactivateAllPrayers();
            Statistics.Normalise();
            Movement.Teleport(Teleport.Create(Area.Script.GetRespawnLocation(this)));
            Movement.Unlock(true);
            OnSpawn();
        }

        /// <summary>
        /// Called when [skill experience gain].
        /// </summary>
        /// <param name="skillID">The skill identifier.</param>
        /// <param name="currentExperience">The current experience.</param>
        public void OnSkillExperienceGain(int skillID, double currentExperience)
        {
            if (currentExperience >= StatisticsConstants.MaximumExperience)
            {
                // TODO
                //var database = ServiceLocator.Current.GetInstance<ISqlDatabaseManager>();
                //database.ExecuteAsync(new ActivityLogQuery(MasterId,
                //    "Max-Experience",
                //    "I have achieved 200 million XP in " + StatisticsConstants.SkillNames[skillID] + "."));
                var announcement = ServiceProvider.GetRequiredService<IGameMessageService>();
                announcement.MessageAsync(DisplayName + " has just achieved 200 million XP in " + StatisticsConstants.SkillNames[skillID] + ".",
                    GameMessageType.WorldSpecific,
                    DisplayName);
            }
        }

        /// <summary>
        /// Poison's this character by given amount.
        /// If amount is lower than 10 the character is then unpoisoned.
        /// </summary>
        /// <param name="amount">Amount of poison strength.</param>
        /// <returns>If character was poisoned sucessfully.</returns>
        public override bool Poison(short amount)
        {
            if (HasState(StateType.ResistPoison)) return false;
            Statistics.SetPoisonAmount(amount);
            return true;
        }

        /// <summary>
        /// Injurt's this character for given amount of ticks.
        /// </summary>
        /// <param name="ticks">Amount of ticks character will be injured.</param>
        public void Injure(int ticks) => AddState(new State(StateType.Injured, ticks));

        /// <summary>
        /// Renders the skull.
        /// </summary>
        /// <param name="icon">The icon.</param>
        /// <param name="ticks">The amount of ticks left (NOT THE TOTAL TICK COUNT).</param>
        /// <returns></returns>
        public bool RenderSkull(SkullIcon icon, int ticks)
        {
            var scripts = GetScripts();
            if (scripts.Any(script => !script.CanRenderSkull(icon)))
            {
                return false;
            }

            if (icon != SkullIcon.DefaultSkull)
            {
                return false;
            }

            AddState(new State(StateType.DefaultSkulled, ticks));
            return true;
        }

        /// <summary>
        /// Set's character minimap dot.
        /// </summary>
        /// <param name="dot">New minimap dot type.</param>
        public void SetMinimapDot(MiniMapDot dot)
        {
            MiniMapDot = dot;
            RenderInformation.ScheduleFlagUpdate(UpdateFlags.OrangeMiniMapDot);
            RenderInformation.ScheduleFlagUpdate(UpdateFlags.PWordMiniMapDot);
        }

        /// <summary>
        /// Determines whether this instance has script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script type.</typeparam>
        /// <returns></returns>
        public bool HasScript<TScriptType>() where TScriptType : class, ICharacterScript
        {
            var type = typeof(TScriptType);
            return _scripts.ContainsKey(type);
        }

        /// <summary>
        /// Gets the script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script type.</typeparam>
        /// <returns></returns>
        public TScriptType? GetScript<TScriptType>() where TScriptType : class, ICharacterScript
        {
            var type = typeof(TScriptType);
            if (_scripts.TryGetValue(type, out var script)) return (TScriptType)script;
            return default;
        }

        /// <summary>
        /// Adds the script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script type.</typeparam>
        /// <returns></returns>
        public TScriptType AddScript<TScriptType>() where TScriptType : class, ICharacterScript => AddScript(ServiceProvider.GetRequiredService<TScriptType>());

        /// <summary>
        /// Loads the script.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <returns>Null if there was already a script with the same type.</returns>
        /// <exception cref="System.Exception">Tried to add a non-initialized CharacterScript.</exception>
        public TScriptType AddScript<TScriptType>(TScriptType script) where TScriptType : class, ICharacterScript
        {
            if (_scripts.ContainsKey(script.GetType()))
            {
                return (TScriptType)_scripts[script.GetType()];
            }

            _scripts.Add(script.GetType(), script);
            return script;
        }

        /// <summary>
        /// Removes the script.
        /// </summary>
        /// <typeparam name="TScriptType">The type of the script.</typeparam>
        /// <returns>If the script was successfully removed.</returns>
        public bool TryRemoveScript<TScriptType>() where TScriptType : class, ICharacterScript
        {
            var type = typeof(TScriptType);
            if (!_scripts.TryGetValue(type, out var script))
            {
                return false;
            }

            if (!_scripts.Remove(type))
            {
                return false;
            }

            script.OnRemove();
            return true;

        }

        /// <summary>
        /// Gets the scripts.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ICharacterScript> GetScripts() => _scripts.Values;
    }
}