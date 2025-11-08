using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Services.GameWorld.Model.Widgets;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Represents characters interfaces.
    /// </summary>
    public class WidgetContainer : IWidgetContainer
    {
        /// <summary>
        /// Contains owner of this class.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// Contains opened interfaces.
        /// </summary>
        private readonly List<IWidget> _openedInterfaces = [];

        /// <summary>
        /// Contains character gameframe.
        /// </summary>
        /// <value>The current frame.</value>
        public IWidget? CurrentFrame { get; private set; }

        /// <summary>
        /// Contains current string input listener.
        /// </summary>
        /// <value>The string input handler.</value>
        public OnStringInput? StringInputHandler { get; set; }

        /// <summary>
        /// Contains current int input listener.
        /// </summary>
        /// <value>The int input handler.</value>
        public OnIntInput? IntInputHandler { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public IReadOnlyList<IWidget> Widgets => _openedInterfaces;

        /// <summary>
        /// Construct's new interfaces container.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public WidgetContainer(ICharacter owner) => _owner = owner;

        /// <summary>
        /// Opens standart interface.
        /// </summary>
        /// <param name="interfaceID">Id of the interface to open.</param>
        /// <param name="transparency">The transparency.</param>
        /// <param name="script">Script which should be loaded to opened interface.</param>
        /// <param name="interrupt">Wheter interrupt() should be called.</param>
        /// <returns>If interface was sucessfully opened.</returns>
        public bool OpenWidget(int interfaceID, int transparency, IWidgetScript script, bool interrupt)
        {
            if (interrupt)
            {
                _owner.Interrupt(this);
            }

            if (CurrentFrame == null || CurrentFrame.Id != (int)InterfaceIds.FixedFrame && CurrentFrame.Id != (int)InterfaceIds.ResizedFrame)
            {
                return false;
            }

            OpenWidget(new Widget(_owner,
                interfaceID,
                CurrentFrame.Id,
                _owner.GameClient.IsScreenFixed ? (short)InterfaceSlots.FixedMainInterfaceSlot : (short)InterfaceSlots.ResizedMainInterfaceSlot,
                transparency,
                script));
            return true;
        }

        /// <summary>
        /// Opens standart interface.
        /// </summary>
        /// <param name="interfaceID">Id of the interface to open.</param>
        /// <param name="parentSlot">The parent slot.</param>
        /// <param name="transparency">The transparency.</param>
        /// <param name="script">Script which should be loaded to opened interface.</param>
        /// <param name="interrupt">Wheter interrupt() should be called.</param>
        /// <returns>
        /// If interface was sucessfully opened.
        /// </returns>
        public bool OpenWidget(int interfaceID, int parentSlot, int transparency, IWidgetScript script, bool interrupt)
        {
            if (interrupt)
            {
                _owner.Interrupt(this);
            }

            if (CurrentFrame == null || CurrentFrame.Id != (int)InterfaceIds.FixedFrame && CurrentFrame.Id != (int)InterfaceIds.ResizedFrame)
            {
                return false;
            }

            OpenWidget(new Widget(_owner, interfaceID, CurrentFrame.Id, parentSlot, transparency, script));
            return true;
        }

        /// <summary>
        /// Opens the standart interface.
        /// </summary>
        /// <param name="interfaceID">The interface Id.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="parentSlot">The parent slot.</param>
        /// <param name="transparency">The transparency.</param>
        /// <param name="script">The script.</param>
        /// <param name="interrupt">if set to <c>true</c> [interrupt].</param>
        /// <returns></returns>
        public bool OpenWidget(int interfaceID, IWidget parent, int parentSlot, int transparency, IWidgetScript script, bool interrupt)
        {
            if (interrupt)
            {
                _owner.Interrupt(this);
            }

            OpenWidget(new Widget(_owner, interfaceID, parent.Id, parentSlot, transparency, script));
            return true;
        }

        /// <summary>
        /// Opens the standart dialogue.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="interrupt">if set to <c>true</c> [interrupt].</param>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public bool OpenDialogue(IDialogueScript script, bool interrupt, IRuneObject? source = null)
        {
            if (interrupt)
            {
                _owner.Interrupt(this);
            }

            var chatbox = GetOpenWidget((int)InterfaceIds.ChatboxFrame);
            if (chatbox == null)
            {
                return false;
            }

            script.SetSource(source);

            OpenWidget(new Widget(_owner, 0, chatbox.Id, (int)InterfaceSlots.ChatboxOverlay, 0, script, source), false);
            return true;
        }

        /// <summary>
        /// Open's chatbox overlay interface (Such as dialogue).
        /// </summary>
        /// <param name="interfaceID">Id of the overlay interface.</param>
        /// <param name="transparency">Transparency of the interface.</param>
        /// <param name="script">Script for the overlay interface.</param>
        /// <param name="interrupt">if set to <c>true</c> [interrupt].</param>
        /// <param name="source">The source.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        public bool OpenChatboxOverlay(int interfaceID, int transparency, IDialogueScript script, bool interrupt, IRuneObject? source = null)
        {
            if (interrupt)
            {
                _owner.Interrupt(this);
            }

            var chatbox = GetOpenWidget((int)InterfaceIds.ChatboxFrame);
            if (chatbox == null)
            {
                return false;
            }

            script.SetSource(source);

            OpenWidget(new Widget(_owner, interfaceID, chatbox.Id, (int)InterfaceSlots.ChatboxOverlay, transparency, script, source));
            return true;
        }

        /// <summary>
        /// Closes chatbox overlay.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool CloseChatboxOverlay()
        {
            var chatbox = GetOpenWidget((int)InterfaceIds.ChatboxFrame);
            var overlay = chatbox?.GetChild((int)InterfaceSlots.ChatboxOverlay);
            if (overlay == null)
            {
                return false;
            }

            CloseWidget(overlay);
            return true;
        }

        /// <summary>
        /// Open's custom inventory interface.
        /// </summary>
        /// <param name="interfaceID">Id of the inventory interface.</param>
        /// <param name="transparency">Transparency of the interface.</param>
        /// <param name="script">Script for custom inv interface.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool OpenInventoryOverlay(int interfaceID, int transparency, IWidgetScript script)
        {
            if (CurrentFrame == null || CurrentFrame.Id != (int)InterfaceIds.FixedFrame && CurrentFrame.Id != (int)InterfaceIds.ResizedFrame)
            {
                return false;
            }

            OpenWidget(new Widget(_owner,
                interfaceID,
                CurrentFrame.Id,
                _owner.GameClient.IsScreenFixed ? (int)InterfaceSlots.FixedInventoryOverlay : (int)InterfaceSlots.ResizedInventoryOverlay,
                transparency,
                script));
            return true;
        }

        /// <summary>
        /// Open's custom inventory interface.
        /// </summary>
        /// <param name="toOpen">To open.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool OpenInventoryOverlay(IWidget toOpen)
        {
            if (CurrentFrame == null || CurrentFrame.Id != (int)InterfaceIds.FixedFrame && CurrentFrame.Id != (int)InterfaceIds.ResizedFrame)
            {
                return false;
            }

            OpenWidget(toOpen);
            return true;
        }

        /// <summary>
        /// Closes inventory overlay.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool CloseInventoryOverlay()
        {
            if (CurrentFrame == null || CurrentFrame.Id != (int)InterfaceIds.FixedFrame && CurrentFrame.Id != (int)InterfaceIds.ResizedFrame)
            {
                return false;
            }

            var overlay = CurrentFrame.GetChild(_owner.GameClient.IsScreenFixed
                ? (int)InterfaceSlots.FixedInventoryOverlay
                : (int)InterfaceSlots.ResizedInventoryOverlay);
            if (overlay == null)
            {
                return false;
            }

            CloseWidget(overlay);
            return true;
        }

        /// <summary>
        /// Close's all interfaces.
        /// </summary>
        public void CloseAll()
        {
            foreach (var i in _openedInterfaces)
            {
                CloseWidget(i);
            }
        }

        /// <summary>
        /// Opens frame for character.
        /// </summary>
        /// <param name="frame">The frame.</param>
        /// <exception cref="Exception"></exception>
        public void OpenFrame(IWidget frame)
        {
            if (!IsValidInterfaceID(frame.Id))
            {
                return;
            }

            if (!frame.IsFrame)
            {
                return;
            }

            var forceRedraw = false;
            if (CurrentFrame != null)
            {
                CloseWidget(CurrentFrame, false);
                forceRedraw = true;
            }

            if (!AddInterface(frame))
            {
                throw new Exception("Couldn't add frame!");
            }

            CurrentFrame = frame;
            _owner.Session.SendMessage(new DrawFrameComponentMessage
            {
                Id = frame.Id, ForceRedraw = forceRedraw
            });
            frame.OnOpen();
        }

        /// <summary>
        /// Opens the interface.
        /// </summary>
        /// <param name="interface">The interface.</param>
        public void OpenWidget(IWidget @interface) => OpenWidget(@interface, true);

        /// <summary>
        /// Open's interface for character.
        /// </summary>
        /// <param name="toOpen">To open.</param>
        /// <param name="refresh">if set to <c>true</c> [refresh].</param>
        /// <exception cref="Exception">Couldn't add interface!</exception>
        private void OpenWidget(IWidget toOpen, bool refresh)
        {
            if (toOpen.IsFrame)
            {
                return;
            }

            if (IsOpened(toOpen))
            {
                CloseWidget(GetOpenWidget(toOpen.Id)!, false);
            }

            var parent = GetOpenWidget(toOpen.ParentId);
            if (parent == null)
            {
                return;
            }

            if (parent.SlotUsed(toOpen.ParentSlot))
            {
                CloseWidget(parent.GetChild(toOpen.ParentSlot)!, false);
            }

            if (!AddInterface(toOpen))
            {
                throw new Exception("Couldn't add interface!");
            }

            parent.AttachChildren(toOpen, toOpen.ParentSlot);
            if (refresh)
            {
                _owner.Session.SendMessage(new DrawInterfaceComponentMessage
                {
                    Id = toOpen.Id, ParentId = parent.Id, ParentSlot = toOpen.ParentSlot, Transparency = toOpen.Transparency
                });
            }

            toOpen.OnOpen();
            _owner.EventManager.SendEvent(new InterfaceOpenedEvent(_owner, toOpen));
        }

        /// <summary>
        /// Closes the interface.
        /// </summary>
        /// <param name="interface">The interface.</param>
        public void CloseWidget(IWidget @interface) => CloseWidget(@interface, true);

        /// <summary>
        /// Closes interface for character.
        /// </summary>
        /// <param name="toClose">To close.</param>
        /// <param name="refresh">if set to <c>true</c> [refresh].</param>
        private void CloseWidget(IWidget toClose, bool refresh)
        {
            if (!RemoveInterface(toClose))
            {
                return;
            }


            // First off , close it's childs.
            foreach (var child in toClose.GetAllChildren())
            {
                CloseWidget(child, refresh);
            }

            // Remove ourselves from parent.
            var parent = GetOpenWidget(toClose.ParentId);
            parent?.DetachChildren(toClose.ParentSlot);
            if (!toClose.IsFrame)
            {
                if (refresh && parent != null)
                {
                    _owner.Session.SendMessage(new RemoveInterfaceComponentMessage
                    {
                        ParentId = parent.Id, ParentSlot = toClose.ParentSlot
                    });
                }
            }
            else
            {
                CurrentFrame = null;
            }

            toClose.OnClose();
            _owner.EventManager.SendEvent(new InterfaceClosedEvent(_owner, toClose));
        }

        /// <summary>
        /// Switches the dialogue, primarely used in dialogue conversations.
        /// </summary>
        /// <param name="interface">From.</param>
        /// <param name="interfaceID">The interface Id.</param>
        /// <exception cref="Exception"></exception>
        public void SetWidgetId(IWidget @interface, int interfaceID)
        {
            if (!IsOpened(@interface))
            {
                return;
            }

            @interface.Id = interfaceID;
            _owner.Session.SendMessage(new DrawInterfaceComponentMessage
            {
                Id = @interface.Id, ParentId = @interface.ParentId, ParentSlot = @interface.ParentSlot, Transparency = @interface.Transparency
            });
        }

        /// <summary>
        /// Put's interface to open interface list.
        /// </summary>
        /// <param name="interface">The rs interface.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool AddInterface(IWidget @interface)
        {
            if (IsOpened(@interface))
            {
                return false;
            }

            _openedInterfaces.Add(@interface);
            return true;
        }

        /// <summary>
        /// Remove's interface.
        /// </summary>
        /// <param name="interface">The interface.</param>
        /// <returns>
        ///   <c>true</c> if XXXX, <c>false</c> otherwise
        /// </returns>
        private bool RemoveInterface(IWidget @interface) => IsOpened(@interface) && _openedInterfaces.Remove(@interface);

        /// <summary>
        /// Get's if given interface is opened.
        /// </summary>
        /// <param name="interface">The interface.</param>
        /// <returns>
        ///   <c>true</c> if the specified interface Id is opened; otherwise, <c>false</c>.
        /// </returns>
        private bool IsOpened(IWidget @interface) => _openedInterfaces.Contains(@interface);

        /// <summary>
        /// Gets the opened dialogue script.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? GetOpenedDialogueScript<T>() where T : IDialogueScript
        {
            var chatbox = GetOpenWidget((int)InterfaceIds.ChatboxFrame);
            var dialogue = chatbox?.GetChild((int)InterfaceSlots.ChatboxOverlay);
            return (T?)dialogue?.Script;
        }

        /// <summary>
        /// Get's open interface.
        /// </summary>
        /// <param name="interfaceID">The Id.</param>
        /// <returns>Interface.</returns>
        public IWidget? GetOpenWidget(int interfaceID) => _openedInterfaces.FirstOrDefault(i => i.Id == interfaceID);

        /// <summary>
        /// Get's if interface Id is valid.
        /// </summary>
        /// <param name="interfaceID">Id of the interface to check.</param>
        /// <returns>Return's if the Id is valid.</returns>
        private bool IsValidInterfaceID(int interfaceID)
        {
            var manager = _owner.ServiceProvider.GetRequiredService<IWidgetScriptProvider>();
            return interfaceID >= 0 && interfaceID < manager.GetInterfacesCount();
        }

        public bool TryGetOpenWidget(int interfaceId, [NotNullWhen(true)] out IWidget? gameInterface)
        {
            gameInterface = GetOpenWidget(interfaceId);
            return gameInterface != null;
        }
    }
}