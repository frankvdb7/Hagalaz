using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// Represents a delegate for handling a click event on a widget component.
    /// </summary>
    /// <param name="componentID">The ID of the component that was clicked.</param>
    /// <param name="buttonClickType">The type of click that was performed.</param>
    /// <param name="info1">Additional client-provided information (e.g., item ID).</param>
    /// <param name="info2">Additional client-provided information (e.g., item slot).</param>
    /// <returns><c>true</c> to indicate the event was handled and should not be propagated; otherwise, <c>false</c>.</returns>
    public delegate bool OnComponentClick(int componentID, ComponentClickType buttonClickType, int info1, int info2);

    /// <summary>
    /// Represents a delegate for handling a drag-and-drop event between widget components.
    /// </summary>
    /// <param name="componentID">The ID of the component that was dragged.</param>
    /// <param name="dragedFromExtraInfo1">Additional information from the source component.</param>
    /// <param name="dragedFromExtraInfo2">Additional information from the source component.</param>
    /// <param name="dragedToInterface">The widget that the component was dragged to.</param>
    /// <param name="dragedToComponentID">The ID of the component that the source was dragged onto.</param>
    /// <param name="dragedToExtraInfo1">Additional information from the destination component.</param>
    /// <param name="dragedToExtraInfo2">Additional information from the destination component.</param>
    /// <returns><c>true</c> to indicate the event was handled and should not be propagated; otherwise, <c>false</c>.</returns>
    public delegate bool OnComponentDragged(int componentID, int dragedFromExtraInfo1, int dragedFromExtraInfo2, IWidget dragedToInterface, int dragedToComponentID, int dragedToExtraInfo1, int dragedToExtraInfo2);

    /// <summary>
    /// Represents a delegate for handling a "use component on creature" event.
    /// </summary>
    /// <param name="componentID">The ID of the component that was used.</param>
    /// <param name="usedOn">The creature the component was used on.</param>
    /// <param name="shouldRun">A value indicating whether the character should force-run to the target.</param>
    /// <param name="info1">Additional client-provided information.</param>
    /// <param name="info2">Additional client-provided information.</param>
    /// <returns><c>true</c> to indicate the event was handled and should not be propagated; otherwise, <c>false</c>.</returns>
    public delegate bool OnComponentUsedOnCreature(int componentID, ICreature usedOn, bool shouldRun, int info1, int info2);

    /// <summary>
    /// Represents a delegate for handling a "use component on game object" event.
    /// </summary>
    /// <param name="componentID">The ID of the component that was used.</param>
    /// <param name="usedOn">The game object the component was used on.</param>
    /// <param name="shouldRun">A value indicating whether the character should force-run to the target.</param>
    /// <param name="info1">Additional client-provided information.</param>
    /// <param name="info2">Additional client-provided information.</param>
    /// <returns><c>true</c> to indicate the event was handled and should not be propagated; otherwise, <c>false</c>.</returns>
    public delegate bool OnComponentUsedOnGameObject(int componentID, IGameObject usedOn, bool shouldRun, int info1, int info2);

    /// <summary>
    /// Represents a delegate for handling a "use component on ground item" event.
    /// </summary>
    /// <param name="componentID">The ID of the component that was used.</param>
    /// <param name="usedOn">The ground item the component was used on.</param>
    /// <param name="shouldRun">A value indicating whether the character should force-run to the target.</param>
    /// <param name="info1">Additional client-provided information.</param>
    /// <param name="info2">Additional client-provided information.</param>
    /// <returns><c>true</c> to indicate the event was handled and should not be propagated; otherwise, <c>false</c>.</returns>
    public delegate bool OnComponentUsedOnGroundItem(int componentID, IGroundItem usedOn, bool shouldRun, int info1, int info2);

    /// <summary>
    /// Represents a delegate for handling a "use component on component" event.
    /// </summary>
    /// <param name="componentID">The ID of the component that was used.</param>
    /// <param name="info1">Additional client-provided information.</param>
    /// <param name="info2">Additional client-provided information.</param>
    /// <param name="info3">Additional client-provided information.</param>
    /// <param name="info4">Additional client-provided information.</param>
    /// <returns><c>true</c> to indicate the event was handled and should not be propagated; otherwise, <c>false</c>.</returns>
    public delegate bool OnComponentUsedOnComponent(int componentID, int info1, int info2, int info3, int info4);

    /// <summary>
    /// Represents a delegate for handling a click event within a dialogue.
    /// </summary>
    /// <param name="stage">The current stage of the dialogue.</param>
    /// <param name="extraData1">Additional data associated with the click.</param>
    /// <returns><c>true</c> to indicate the event was handled and the dialogue should not progress automatically; otherwise, <c>false</c>.</returns>
    public delegate bool OnDialogueClick(int stage, int extraData1);

    /// <summary>
    /// Represents a delegate for handling string input from a dialog box.
    /// </summary>
    /// <param name="str">The string that was entered.</param>
    public delegate void OnStringInput(string str);

    /// <summary>
    /// Represents a delegate for handling integer input from a dialog box.
    /// </summary>
    /// <param name="value">The integer that was entered.</param>
    public delegate void OnIntInput(int value);
}