using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Hagalaz.Game.Abstractions.Model.Widgets;

namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for managing a character's user interface, including opening, closing, and interacting with widgets.
    /// </summary>
    public interface IWidgetContainer
    {
        /// <summary>
        /// Gets the main top-level widget, or "frame," that is currently open.
        /// </summary>
        IWidget CurrentFrame { get; }
        /// <summary>
        /// Gets or sets the current handler for string input dialogs.
        /// </summary>
        OnStringInput? StringInputHandler { get; set; }
        /// <summary>
        /// Gets or sets the current handler for integer input dialogs.
        /// </summary>
        OnIntInput? IntInputHandler { get; set; }
        /// <summary>
        /// Gets a read-only list of all currently open widgets.
        /// </summary>
        IReadOnlyList<IWidget> Widgets { get; }
        /// <summary>
        /// Opens a new top-level frame widget.
        /// </summary>
        /// <param name="frame">The frame widget to open.</param>
        void OpenFrame(IWidget frame);
        /// <summary>
        /// Opens a standard widget.
        /// </summary>
        /// <param name="widget">The widget to open.</param>
        void OpenWidget(IWidget widget);
        /// <summary>
        /// Gets the script of the currently open dialogue, if it is of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the dialogue script to get.</typeparam>
        /// <returns>The dialogue script instance if found and of the correct type; otherwise, <c>null</c>.</returns>
        T? GetOpenedDialogueScript<T>() where T : IDialogueScript;
        /// <summary>
        /// Opens a widget as an overlay on top of the inventory.
        /// </summary>
        /// <param name="widgetId">The ID of the widget to open.</param>
        /// <param name="transparency">The transparency level of the overlay.</param>
        /// <param name="script">The script to attach to the widget.</param>
        /// <returns><c>true</c> if the overlay was successfully opened; otherwise, <c>false</c>.</returns>
        bool OpenInventoryOverlay(int widgetId, int transparency, IWidgetScript script);
        /// <summary>
        /// Closes any widget that is currently open as a chatbox overlay.
        /// </summary>
        /// <returns><c>true</c> if a chatbox overlay was successfully closed; otherwise, <c>false</c>.</returns>
        bool CloseChatboxOverlay();
        /// <summary>
        /// Closes a specific widget.
        /// </summary>
        /// <param name="widget">The widget to close.</param>
        void CloseWidget(IWidget widget);
        /// <summary>
        /// Opens a standard widget with a script.
        /// </summary>
        /// <param name="widgetId">The ID of the widget to open.</param>
        /// <param name="transparency">The transparency level.</param>
        /// <param name="script">The script to attach to the widget.</param>
        /// <param name="interrupt">A value indicating whether to interrupt the character's current action.</param>
        /// <returns><c>true</c> if the widget was successfully opened; otherwise, <c>false</c>.</returns>
        bool OpenWidget(int widgetId, int transparency, IWidgetScript script, bool interrupt);
        /// <summary>
        /// Opens a standard widget as a child of another component.
        /// </summary>
        /// <param name="widgetId">The ID of the widget to open.</param>
        /// <param name="parentSlot">The component slot within the parent widget where this widget should be opened.</param>
        /// <param name="transparency">The transparency level.</param>
        /// <param name="script">The script to attach to the widget.</param>
        /// <param name="interrupt">A value indicating whether to interrupt the character's current action.</param>
        /// <returns><c>true</c> if the widget was successfully opened; otherwise, <c>false</c>.</returns>
        bool OpenWidget(int widgetId, int parentSlot, int transparency, IWidgetScript script, bool interrupt);
        /// <summary>
        /// Opens a standard widget as a child of another widget.
        /// </summary>
        /// <param name="widgetId">The ID of the widget to open.</param>
        /// <param name="parent">The parent widget.</param>
        /// <param name="parentSlot">The component slot within the parent widget where this widget should be opened.</param>
        /// <param name="transparency">The transparency level.</param>
        /// <param name="script">The script to attach to the widget.</param>
        /// <param name="interrupt">A value indicating whether to interrupt the character's current action.</param>
        /// <returns><c>true</c> if the widget was successfully opened; otherwise, <c>false</c>.</returns>
        bool OpenWidget(int widgetId, IWidget parent, int parentSlot, int transparency, IWidgetScript script, bool interrupt);
        /// <summary>
        /// Opens a standard dialogue.
        /// </summary>
        /// <param name="script">The script that controls the dialogue.</param>
        /// <param name="interrupt">A value indicating whether to interrupt the character's current action.</param>
        /// <param name="source">The entity that initiated the dialogue, if any.</param>
        /// <returns><c>true</c> if the dialogue was successfully opened; otherwise, <c>false</c>.</returns>
        bool OpenDialogue(IDialogueScript script, bool interrupt, IRuneObject? source = null);
        /// <summary>
        /// Opens a widget as an overlay on top of the chatbox (e.g., a dialogue).
        /// </summary>
        /// <param name="widgetId">The ID of the widget to open.</param>
        /// <param name="transparency">The transparency level of the overlay.</param>
        /// <param name="script">The script to attach to the widget.</param>
        /// <param name="interrupt">A value indicating whether to interrupt the character's current action.</param>
        /// <param name="source">The entity that initiated the action, if any.</param>
        /// <returns><c>true</c> if the overlay was successfully opened; otherwise, <c>false</c>.</returns>
        bool OpenChatboxOverlay(int widgetId, int transparency, IDialogueScript script, bool interrupt, IRuneObject? source = null);
        /// <summary>
        /// Closes all open widgets.
        /// </summary>
        void CloseAll();
        /// <summary>
        /// Changes the ID of a currently open widget, effectively replacing it with another one.
        /// </summary>
        /// <param name="toSwitch">The widget to be replaced.</param>
        /// <param name="widgetId">The ID of the new widget to display.</param>
        void SetWidgetId(IWidget toSwitch, int widgetId);
        /// <summary>
        /// Gets a currently open widget by its ID.
        /// </summary>
        /// <param name="widgetId">The ID of the widget to find.</param>
        /// <returns>The <see cref="IWidget"/> if found; otherwise, <c>null</c>.</returns>
        IWidget? GetOpenWidget(int widgetId);
        /// <summary>
        /// Tries to get a currently open widget by its ID.
        /// </summary>
        /// <param name="widgetId">The ID of the widget to find.</param>
        /// <param name="gameWidget">When this method returns, contains the widget if found; otherwise, null.</param>
        /// <returns><c>true</c> if the widget was found; otherwise, <c>false</c>.</returns>
        bool TryGetOpenWidget(int widgetId, [NotNullWhen(true)] out IWidget? gameWidget);
    }
}
