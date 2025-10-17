using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// Defines the contract for a user interface widget (also known as an interface).
    /// </summary>
    public interface IWidget
    {
        /// <summary>
        /// Gets or sets the unique ID of the widget.
        /// </summary>
        int Id { get; set; }

        /// <summary>
        /// Gets the character who owns this widget.
        /// </summary>
        ICharacter Owner { get; }

        /// <summary>
        /// Gets a value indicating whether this widget is currently open.
        /// </summary>
        bool IsOpened { get; }

        /// <summary>
        /// Gets a value indicating whether this widget is a top-level frame.
        /// </summary>
        bool IsFrame { get; }

        /// <summary>
        /// Gets the ID of the parent widget.
        /// </summary>
        int ParentId { get; }

        /// <summary>
        /// Gets the script that controls the widget's behavior and logic.
        /// </summary>
        IWidgetScript Script { get; }

        /// <summary>
        /// Gets the component slot within the parent widget where this widget is located.
        /// </summary>
        int ParentSlot { get; }

        /// <summary>
        /// Gets the transparency level of the widget.
        /// </summary>
        int Transparency { get; }

        /// <summary>
        /// Attaches a child widget to a specific component slot of this widget.
        /// </summary>
        /// <param name="children">The child widget to attach.</param>
        /// <param name="slot">The component slot to attach to.</param>
        void AttachChildren(IWidget children, int slot);

        /// <summary>
        /// Attaches a click handler to a specific component within this widget.
        /// </summary>
        /// <param name="childID">The ID of the component to listen to.</param>
        /// <param name="handler">The delegate to be executed on click.</param>
        void AttachClickHandler(int childID, OnComponentClick handler);

        /// <summary>
        /// Attaches a drag handler to a specific component within this widget.
        /// </summary>
        /// <param name="childID">The ID of the component to listen to.</param>
        /// <param name="handler">The delegate to be executed on drag.</param>
        void AttachDragHandler(int childID, OnComponentDragged handler);

        /// <summary>
        /// Attaches a "use on game object" handler to a specific component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="handler">The delegate to be executed.</param>
        void AttachUseOnObjectHandler(int childID, OnComponentUsedOnGameObject handler);

        /// <summary>
        /// Attaches a "use on ground item" handler to a specific component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="handler">The delegate to be executed.</param>
        void AttachUseOnGroundItemHandler(int childID, OnComponentUsedOnGroundItem handler);

        /// <summary>
        /// Attaches a "use on component" handler to a specific component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="handler">The delegate to be executed.</param>
        void AttachUseOnComponentHandler(int childID, OnComponentUsedOnComponent handler);

        /// <summary>
        /// Attaches a "use on creature" handler to a specific component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="handler">The delegate to be executed.</param>
        void AttachUseOnCreatureHandler(int childID, OnComponentUsedOnCreature handler);

        /// <summary>
        /// Detaches a child widget from a specific component slot.
        /// </summary>
        /// <param name="slot">The slot from which to detach the child widget.</param>
        void DetachChildren(int slot);

        /// <summary>
        /// Detaches a specific click handler from a component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="handler">The handler to detach.</param>
        void DetachClickHandler(int childID, OnComponentClick handler);

        /// <summary>
        /// Detaches all click handlers from a specific component.
        /// </summary>
        /// <param name="childId">The ID of the component.</param>
        /// <returns><c>true</c> if handlers were successfully detached; otherwise, <c>false</c>.</returns>
        bool DetachClickHandlers(int childId);

        /// <summary>
        /// Gets the child widget attached to a specific slot.
        /// </summary>
        /// <param name="slot">The component slot.</param>
        /// <returns>The child widget if found; otherwise, <c>null</c>.</returns>
        IWidget? GetChild(int slot);

        /// <summary>
        /// Gets a read-only list of all child widgets attached to this widget.
        /// </summary>
        /// <returns>A list of child widgets.</returns>
        IReadOnlyList<IWidget> GetAllChildren();

        /// <summary>
        /// Checks if this widget can be interrupted by other actions.
        /// </summary>
        /// <returns><c>true</c> if the widget can be interrupted; otherwise, <c>false</c>.</returns>
        bool CanInterrupt();

        /// <summary>
        /// Closes this widget.
        /// </summary>
        void Close();

        /// <summary>
        /// Checks if a specific component slot is currently occupied by a child widget.
        /// </summary>
        /// <param name="slot">The slot to check.</param>
        /// <returns><c>true</c> if the slot is used; otherwise, <c>false</c>.</returns>
        bool SlotUsed(int slot);

        /// <summary>
        /// A callback executed when the client's display mode changes.
        /// </summary>
        void OnDisplayChanged();

        /// <summary>
        /// A callback executed when the widget is opened.
        /// </summary>
        void OnOpen();

        /// <summary>
        /// A callback executed when the widget is closed.
        /// </summary>
        void OnClose();

        /// <summary>
        /// Draws an item model on a specific component of the widget.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="itemID">The ID of the item to draw.</param>
        /// <param name="count">The quantity of the item to display.</param>
        void DrawItem(int childID, int itemID, int count);

        /// <summary>
        /// Draws a string of text on a specific component of the widget.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="str">The string to draw.</param>
        void DrawString(int childID, string str);

        /// <summary>
        /// Sets the visibility of a specific component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="visible">A value indicating whether the component should be visible.</param>
        void SetVisible(int childID, bool visible);

        /// <summary>
        /// Sets the available right-click options for a component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="min">The minimum index of the option range.</param>
        /// <param name="max">The maximum index of the option range.</param>
        /// <param name="value">The options bitmask.</param>
        void SetOptions(int childID, int min, int max, int value);

        /// <summary>
        /// Sets the animation for a model on a component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="animationID">The ID of the animation to play.</param>
        void SetAnimation(int childID, int animationID);

        /// <summary>
        /// Draws a model on a specific component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="modelID">The ID of the model to draw.</param>
        void DrawModel(int childID, int modelID);

        /// <summary>
        /// Draws a sprite on a specific component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="spriteID">The ID of the sprite to draw.</param>
        void DrawSprite(int childID, int spriteID);

        /// <summary>
        /// Draws the owner character's head model on a component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        void DrawCharacterHead(int childID);

        /// <summary>
        /// Draws a specific character's head model on a component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="character">The character whose head to draw.</param>
        void DrawCharacterHead(int childID, ICharacter character);

        /// <summary>
        /// Draws an NPC's head model on a component.
        /// </summary>
        /// <param name="childID">The ID of the component.</param>
        /// <param name="npcID">The ID of the NPC whose head to draw.</param>
        void DrawNpcHead(int childID, int npcID);

        /// <summary>
        /// A callback executed when a component is clicked.
        /// </summary>
        /// <param name="childID">The ID of the clicked component.</param>
        /// <param name="clickType">The type of click performed.</param>
        /// <param name="extraInfo1">Additional client-provided information.</param>
        /// <param name="extraInfo2">Additional client-provided information.</param>
        void OnComponentClick(int childID, ComponentClickType clickType, int extraInfo1, int extraInfo2);

        /// <summary>
        /// A callback executed when a component is used on a ground item.
        /// </summary>
        /// <param name="childID">The ID of the component that was used.</param>
        /// <param name="usedOn">The ground item the component was used on.</param>
        /// <param name="shouldRun">A value indicating whether the character should force-run to the target.</param>
        /// <param name="extraInfo1">Additional client-provided information.</param>
        /// <param name="extraInfo2">Additional client-provided information.</param>
        void OnComponentUsedOnGroundItem(int childID, IGroundItem usedOn, bool shouldRun, int extraInfo1, int extraInfo2);

        /// <summary>
        /// A callback executed when a component is used on a creature.
        /// </summary>
        /// <param name="childID">The ID of the component that was used.</param>
        /// <param name="usedOn">The creature the component was used on.</param>
        /// <param name="shouldRun">A value indicating whether the character should force-run to the target.</param>
        /// <param name="extraInfo1">Additional client-provided information.</param>
        /// <param name="extraInfo2">Additional client-provided information.</param>
        void OnComponentUsedOnCreature(int childID, ICreature usedOn, bool shouldRun, int extraInfo1, int extraInfo2);

        /// <summary>
        /// A callback executed when a component is dragged.
        /// </summary>
        /// <param name="childID">The ID of the component that was dragged.</param>
        /// <param name="dragedFromExtraInfo1">Additional information from the source component.</param>
        /// <param name="dragedFromExtraInfo2">Additional information from the source component.</param>
        /// <param name="dragedToInterface">The widget that the component was dragged to.</param>
        /// <param name="dragedToChildID">The ID of the component that the source was dragged onto.</param>
        /// <param name="dragedToExtraInfo1">Additional information from the destination component.</param>
        /// <param name="dragedToExtraInfo2">Additional information from the destination component.</param>
        void OnComponentDrag(int childID, int dragedFromExtraInfo1, int dragedFromExtraInfo2, IWidget dragedToInterface, int dragedToChildID, int dragedToExtraInfo1, int dragedToExtraInfo2);

        /// <summary>
        /// A callback executed when a component is used on another component.
        /// </summary>
        /// <param name="childID">The ID of the component that was used.</param>
        /// <param name="extraInfo1">Additional client-provided information.</param>
        /// <param name="extraInfo2">Additional client-provided information.</param>
        /// <param name="extraInfo3">Additional client-provided information.</param>
        /// <param name="extraInfo4">Additional client-provided information.</param>
        /// <returns><c>true</c> if the event was handled; otherwise, <c>false</c>.</returns>
        bool OnComponentUsedOnComponent(int childID, int extraInfo1, int extraInfo2, int extraInfo3, int extraInfo4);

        /// <summary>
        /// A callback executed when a component is used on a game object.
        /// </summary>
        /// <param name="childID">The ID of the component that was used.</param>
        /// <param name="usedOn">The game object the component was used on.</param>
        /// <param name="shouldRun">A value indicating whether the character should force-run to the target.</param>
        /// <param name="extraInfo1">Additional client-provided information.</param>
        /// <param name="extraInfo2">Additional client-provided information.</param>
        void OnComponentUsedOnGameObject(int childID, IGameObject usedOn, bool shouldRun, int extraInfo1, int extraInfo2);
    }
}