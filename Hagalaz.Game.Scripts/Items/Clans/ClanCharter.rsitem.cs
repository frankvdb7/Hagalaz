using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Request;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Dialogues.Generic;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;
using Hagalaz.Game.Scripts.Model.Items;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Items.Clans
{
    [ItemScriptMetaData([20707])]
    public class ClanCharter : ItemScript
    {
        /// <summary>
        ///     Items the clicked in inventory.
        /// </summary>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="item">The item.</param>
        /// <param name="character">The character.</param>
        public override void ItemClickedInInventory(ComponentClickType clickType, IItem item, ICharacter character)
        {
            if (clickType == ComponentClickType.LeftClick)
            {
                if (character.HasClan())
                {
                    character.SendChatMessage("You're already in a clan!");
                    return;
                }

                var dialogue = character.ServiceProvider.GetRequiredService<ClanCharterDialogue>();
                character.Widgets.OpenDialogue(dialogue, true, item);
                return;
            }

            if (clickType == ComponentClickType.Option7Click)
            {
                if (character.HasClan())
                {
                    character.SendChatMessage("You're already in a clan!");
                    return;
                }

                var script = character.GetScript<ClanFounding>();
                script?.ClearFounders();
                character.TryRemoveScript<ClanFounding>();
                character.SendChatMessage("Your clan charter has been cleared.");
                return;
            }

            base.ItemClickedInInventory(clickType, item, character);
        }

        /// <summary>
        ///     Uses the item on character.
        /// </summary>
        /// <param name="used">The used.</param>
        /// <param name="usedOn">The used on.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        public override bool UseItemOnCharacter(IItem used, ICharacter usedOn, ICharacter character)
        {
            if (character.HasClan())
            {
                character.SendChatMessage("You're already in a clan!");
                return true;
            }

            if (new AllowClanInviteEvent(usedOn).Send())
            {
                if (usedOn.Inventory.Contains(20707))
                {
                    usedOn.SendChatMessage(character.DisplayName + " has attempted to add you to a clan charter, but you are also carrying a charter."
                                                                 + " If you wish to be recruited by them, please remove your charter.");
                    character.SendChatMessage(usedOn.DisplayName + " is currently carrying a charter and cannot be recruited");
                    return true;
                }

                var requestBuilder = character.ServiceProvider.GetRequiredService<IChatMessageRequestBuilder>();
                var yesNoDialogueScript = character.ServiceProvider.GetRequiredService<YesNoDialogueScript>();
                yesNoDialogueScript.Question = "Confirm asking " + usedOn.DisplayName + " to help found your clan?";
                yesNoDialogueScript.Callback = yes =>
                {
                    if (yes)
                    {
                        var req = requestBuilder
                            .Create()
                            .WithSource(character)
                            .WithSourceMessage("Sending request to " + usedOn.DisplayName + " to help found your clan.")
                            .WithTarget(usedOn)
                            .WithTargetMessage(character.DisplayName + " is asking you to help found a clan.")
                            .WithType(ChatMessageType.ClanRequest)
                            .WithOption(option =>
                                option
                                    .WithName("null")
                                    .WithType(CharacterClickType.Option9Click)
                                    .WithAction(() =>
                                    {
                                        var foundInviteScript = usedOn.ServiceProvider.GetRequiredService<FoundInviteScript>();
                                        foundInviteScript.Inviter = character;
                                        usedOn.Widgets.OpenWidget(1093, 0, foundInviteScript, true);
                                    }))
                            .Build();
                        req.TrySend();
                    }

                    character.Widgets.CloseChatboxOverlay();
                };

                character.Widgets.OpenDialogue(yesNoDialogueScript, true);
            }

            return true;
        }

        /// <summary>
        /// </summary>
        public class AllowClanInviteEvent : CharacterEvent
        {
            /// <summary>
            ///     Initializes a new instance of the <see cref="AllowClanInviteEvent" /> class.
            /// </summary>
            /// <param name="character">The character.</param>
            public AllowClanInviteEvent(ICharacter character)
                : base(character) { }
        }

        /// <summary>
        /// </summary>
        public class ClanFounding : CharacterScriptBase
        {
            /// <summary>
            ///     The founders.
            /// </summary>
            private readonly List<ICharacter> _founders = new List<ICharacter>(5);

            public ClanFounding(ICharacterContextAccessor contextAccessor) : base(contextAccessor) { }

            /// <summary>
            ///     Gets the founders.
            /// </summary>
            /// <value>
            ///     The founders.
            /// </value>
            public IEnumerable<ICharacter> Founders => _founders.AsEnumerable();

            /// <summary>
            ///     Initializes this instance.
            /// </summary>
            protected override void Initialize() => _founders.Add(Character);

            /// <summary>
            ///     Gets the founder names.
            /// </summary>
            /// <returns></returns>
            public IEnumerable<string> GetFounderNames()
            {
                var names = new List<string>();
                foreach (var founder in _founders)
                {
                    names.Add(founder.DisplayName);
                }

                return names;
            }

            /// <summary>
            ///     Gets the founder count.
            /// </summary>
            /// <returns></returns>
            public int GetFounderCount() => _founders.Count;

            /// <summary>
            ///     Adds the founder.
            /// </summary>
            /// <param name="character">The character.</param>
            public void AddFounder(ICharacter character)
            {
                if (!_founders.Contains(character))
                {
                    _founders.Add(character);
                }

                Character.SendChatMessage(character.DisplayName + " has accepted the invitation to found a clan with you!");
                character.SendChatMessage("You have accepted the invitation of " + Character.DisplayName + " to found a clan!");
                if (HasAllFounders())
                {
                    Character.SendChatMessage("You have all the founders you need to start a clan!");
                }

                EventHappened cevent = null;
                cevent = character.RegisterEventHandler(new EventHappened<AllowClanInviteEvent>(e =>
                {
                    if (!_founders.Contains(character))
                    {
                        character.UnregisterEventHandler<AllowClanInviteEvent>(cevent);
                        return false;
                    }

                    Character.SendChatMessage(character.DisplayName + " is already a founder on your clan chart!");

                    return true;
                }));
            }

            /// <summary>
            ///     Removes the fouder.
            /// </summary>
            /// <param name="character">The character.</param>
            public bool RemoveFouder(ICharacter character)
            {
                Character.SendChatMessage(character.DisplayName + " has declined the invitation to found a clan with you.");
                return _founders.Remove(character);
            }

            /// <summary>
            ///     Gets the remaining founders.
            /// </summary>
            /// <returns></returns>
            public int GetRemainingFounders() => _founders.Capacity - _founders.Count;

            /// <summary>
            ///     Determines whether [has all founders].
            /// </summary>
            /// <returns></returns>
            public bool HasAllFounders() => _founders.Count == _founders.Capacity;

            /// <summary>
            ///     Clears the founders.
            /// </summary>
            public void ClearFounders()
            {
                var founders = new List<ICharacter>(_founders);
                foreach (var character in founders)
                {
                    if (character == Character)
                    {
                        continue;
                    }

                    character.SendChatMessage(Character.DisplayName + " has declined their invitation to found a clan.");
                    _founders.Remove(character);
                }
            }

            /// <summary>
            ///     Ticks this instance.
            /// </summary>
            public override void Tick()
            {
                var founders = new List<ICharacter>(_founders);
                foreach (var character in founders)
                {
                    if (character.IsDestroyed)
                    {
                        RemoveFouder(character);
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        public class ClanCharterDialogue : ItemDialogueScript
        {
            /// <summary>
            ///     The script
            /// </summary>
            private ClanFounding _script;

            /// <summary>
            ///     Containsclan name handler.
            /// </summary>
            private OnStringInput? _clanNameHandler;

            public ClanCharterDialogue(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

            /// <summary>
            ///     Called when [open].
            /// </summary>
            public override void OnOpen()
            {
                _script = Owner.GetScript<ClanFounding>();
                if (_script == null)
                {
                    _script = Owner.AddScript<ClanFounding>();
                }

                AttachDialogueContinueClickHandler(0,
                    (extraData1, extraData2) =>
                    {
                        if (_script.HasAllFounders())
                        {
                            StandardDialogue("You have all the founders you need to start a clan!");
                            AttachStartClanHandler();
                        }
                        else
                        {
                            if (_script.GetFounderCount() == 1)
                            {
                                StandardDialogue(
                                    "You should start collecting names to add to your charter. To do so, use the charter on a player who is not a member of a clan.");
                                AttachInfoChartHandler();
                            }
                            else
                            {
                                StandardDialogue("You should collect " + _script.GetRemainingFounders() +
                                                 " more names to add to your charter. To do so, use the charter on a player who is not a member of a clan.");
                                AttachViewChartHandler();
                            }
                        }

                        return true;
                    });
            }

            /// <summary>
            ///     Attaches the start clan handler.
            /// </summary>
            private void AttachStartClanHandler() =>
                AttachDialogueContinueClickHandler(1,
                    (extraData1, extraData2) =>
                    {
                        ShowFoundClanDialogue();
                        return true;
                    });

            /// <summary>
            ///     Attaches the information chart handler.
            /// </summary>
            private void AttachInfoChartHandler()
            {
                AttachDialogueContinueClickHandler(1,
                    (extraData1, extraData2) =>
                    {
                        StandardDialogue("Remember that all founders need to remain logged in to complete the founding of a clan.");
                        return true;
                    });
                AttachDialogueContinueClickHandler(2,
                    (extraData1, extraData2) =>
                    {
                        Owner.Widgets.CloseChatboxOverlay();
                        return true;
                    });
            }

            /// <summary>
            ///     Attaches the view chart handler.
            /// </summary>
            private void AttachViewChartHandler() =>
                AttachDialogueContinueClickHandler(1,
                    (extraData1, extraData2) =>
                    {
                        var count = _script.GetFounderCount();
                        var names = _script.GetFounderNames();
                        var name = "names";
                        if (count == 1)
                        {
                            name = "name";
                        }

                        StandardOptionDialogue("Your charter currently contains " + count + " " + name, names.ToArray());

                        OnDialogueClick optionClick = (extra1, extra2) =>
                        {
                            InterfaceInstance.Close();
                            return true;
                        };
                        foreach (var n in names)
                        {
                            AttachDialogueOptionClickHandler(n, optionClick);
                        }

                        return true;
                    });

            /// <summary>
            ///     Shows the found clan dialogue.
            /// </summary>
            private void ShowFoundClanDialogue()
            {
                Owner.Widgets.SetWidgetId(InterfaceInstance, (short)DialogueInterfaces.FoundClanDialog);
                InterfaceInstance.DrawString(8, "Save");
                _clanNameHandler = Owner.Widgets.StringInputHandler = input =>
                {
                    _clanNameHandler = Owner.Widgets.StringInputHandler = null;
                    StandardOptionDialogue("Please confirm your chosen clan name.", input, "Choose a different name.");
                    AttachDialogueOptionClickHandler(input,
                        (e1, e2) =>
                        {
                            //var adapter = ServiceLocator.Current.GetInstance<IMasterConnectionAdapter>();
                            //adapter.SendPacketAsync(new AddClanRequestPacketComposer(Owner, input, _script.Founders));
                            return true;
                        });
                    AttachDialogueOptionClickHandler("Choose a different name.",
                        (e1, e2) =>
                        {
                            ShowFoundClanDialogue();
                            return true;
                        });
                };
            }

            /// <summary>
            ///     Called when [close].
            /// </summary>
            public override void OnClose()
            {
                if (_clanNameHandler != null)
                {
                    if (Owner.Widgets.StringInputHandler == _clanNameHandler)
                    {
                        Owner.Widgets.StringInputHandler = null;
                    }

                    _clanNameHandler = null;
                }
            }
        }

        /// <summary>
        /// </summary>
        public class FoundInviteScript : WidgetScript
        {
            public FoundInviteScript(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

            /// <summary>
            ///     The inviter.
            /// </summary>
            public ICharacter Inviter { get; set; }

            /// <summary>
            ///     The accepted
            /// </summary>
            private bool _accepted;

            /// <summary>
            ///     Called when [open].
            /// </summary>
            public override void OnOpen() =>
                InterfaceInstance.AttachClickHandler(15,
                    (componentID, clickType, extraData1, extraData2) =>
                    {
                        if (clickType == ComponentClickType.SpecialClick)
                        {
                            var script = Inviter.GetOrAddScript<ClanFounding>();
                            script.AddFounder(Owner);
                            _accepted = true;
                            InterfaceInstance.Close();
                            return true;
                        }

                        return false;
                    });

            /// <summary>
            ///     Called when [close].
            /// </summary>
            public override void OnClose()
            {
                if (!_accepted)
                {
                    Inviter.SendChatMessage(Owner.DisplayName + " has declined the invitation to found a clan with you.");
                    Owner.SendChatMessage("You have declined the invitation to found a clan with " + Inviter.DisplayName);
                }
            }
        }
    }
}