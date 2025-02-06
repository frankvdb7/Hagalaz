using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface IWidgetContainer
    {
        /// <summary>
        /// Gets the current frame.
        /// </summary>
        /// <value>
        /// The current frame.
        /// </value>
        IWidget CurrentFrame { get; }
        /// <summary>
        /// Contains current string input listener.
        /// </summary>
        /// <value>The string input handler.</value>
        OnStringInput? StringInputHandler { get; set; }
        /// <summary>
        /// Contains current int input listener.
        /// </summary>
        /// <value>The int input handler.</value>
        OnIntInput? IntInputHandler { get; set; }
        /// <summary>
        /// 
        /// </summary>
        IReadOnlyList<IWidget> Widgets { get; }

        /// <summary>
        /// Opens the frame.
        /// </summary>
        /// <param name="frame">The frame.</param>
        void OpenFrame(IWidget frame);

        /// <summary>
        /// Opens the interface.
        /// </summary>
        /// <param name="interface">The interface.</param>
        void OpenWidget(IWidget @interface);
        /// <summary>
        /// Gets the opened dialogue script.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T? GetOpenedDialogueScript<T>() where T : IDialogueScript;
        /// <summary>
        /// Open's custom inventory interface.
        /// </summary>
        /// <param name="interfaceID">Id of the inventory interface.</param>
        /// <param name="transparency">Transparency of the interface.</param>
        /// <param name="script">Script for custom inv interface.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool OpenInventoryOverlay(int interfaceID, int transparency, IWidgetScript script);
        /// <summary>
        /// Closes chatbox overlay.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        bool CloseChatboxOverlay();

        /// <summary>
        /// Closes the interface.
        /// </summary>
        /// <param name="interface">The interface.</param>
        void CloseWidget(IWidget @interface);
        /// <summary>
        /// Opens standart interface.
        /// </summary>
        /// <param name="interfaceID">Id of the interface to open.</param>
        /// <param name="transparency">The transparency.</param>
        /// <param name="script">Script which should be loaded to opened interface.</param>
        /// <param name="interrupt">Wheter interrupt() should be called.</param>
        /// <returns>If interface was sucessfully opened.</returns>
        bool OpenWidget(int interfaceID, int transparency, IWidgetScript script, bool interrupt);

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
        bool OpenWidget(int interfaceID, int parentSlot, int transparency, IWidgetScript script, bool interrupt);
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
        bool OpenWidget(int interfaceID, IWidget parent, int parentSlot, int transparency, IWidgetScript script, bool interrupt);
        /// <summary>
        /// Opens the standart dialogue.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="interrupt">if set to <c>true</c> [interrupt].</param>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        bool OpenDialogue(IDialogueScript script, bool interrupt, IRuneObject? source = null);
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
        bool OpenChatboxOverlay(int interfaceID, int transparency, IDialogueScript script, bool interrupt, IRuneObject? source = null);
        /// <summary>
        /// Closes all.
        /// </summary>
        void CloseAll();

        /// <summary>
        /// Switches the dialogue, primarely used in dialogue conversations.
        /// </summary>
        /// <param name="toSwitch">From.</param>
        /// <param name="interfaceID">The interface Id.</param>
        /// <exception cref="System.Exception"></exception>
        void SetWidgetId(IWidget toSwitch, int interfaceID);
        /// <summary>
        /// Gets the open interface.
        /// </summary>
        /// <param name="interfaceID">The interface identifier.</param>
        /// <returns></returns>
        IWidget? GetOpenWidget(int interfaceID);

        /// <summary>
        /// Tries to get the open interface.
        /// Returns false if the interface is not opened.
        /// </summary>
        /// <param name="interfaceId"></param>
        /// <param name="gameInterface"></param>
        /// <returns></returns>
        bool TryGetOpenWidget(int interfaceId, [NotNullWhen(true)] out IWidget? gameInterface);
    }
}
