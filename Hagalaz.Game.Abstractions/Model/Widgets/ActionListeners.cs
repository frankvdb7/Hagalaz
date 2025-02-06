using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// Delegate for button click listeners.
    /// </summary>
    /// <param name="componentID">Id of the component that was clicked.</param>
    /// <param name="buttonClickType">Click type that was performed.</param>
    /// <param name="info1">Extra info that client provides.</param>
    /// <param name="info2">Extra info that client provides.</param>
    /// <returns>
    /// Returns true if click was 'caught' and shouldn't be passed to other handlers.
    /// </returns>
    public delegate bool OnComponentClick(int componentID, ComponentClickType buttonClickType, int info1, int info2);

    /// <summary>
    /// Delegate for component drag listeners.
    /// </summary>
    /// <param name="componentID">Id of the component that was draged.</param>
    /// <param name="dragedFromExtraInfo1">The draged from extra info1.</param>
    /// <param name="dragedFromExtraInfo2">The draged from extra info2.</param>
    /// <param name="dragedToInterface">Interface which given component was draged to.</param>
    /// <param name="dragedToComponentID">Id of the component in dragedToInterface which component was draged to.</param>
    /// <param name="dragedToExtraInfo1">The draged to extra info1.</param>
    /// <param name="dragedToExtraInfo2">The draged to extra info2.</param>
    /// <returns>
    /// Returns true if drag was 'caught' and shouldn't be passed to other handlers.
    /// </returns>
    public delegate bool OnComponentDragged(int componentID, int dragedFromExtraInfo1, int dragedFromExtraInfo2, IWidget dragedToInterface, int dragedToComponentID, int dragedToExtraInfo1, int dragedToExtraInfo2);

    /// <summary>
    /// Delegates for use-on listeners.
    /// </summary>
    /// <param name="componentID">Id of the component that was used.</param>
    /// <param name="usedOn">Creature that component was used on.</param>
    /// <param name="shouldRun">Wheter CTRL was pressed.</param>
    /// <param name="info1">Extra info that client provides.</param>
    /// <param name="info2">Extra info that client provides.</param>
    /// <returns>
    /// Returns true if use was 'caught' and shouldn't be passed to other handlers.
    /// </returns>
    public delegate bool OnComponentUsedOnCreature(int componentID, ICreature usedOn, bool shouldRun, int info1, int info2);

    /// <summary>
    /// Delegates for use-on listeners.
    /// </summary>
    /// <param name="componentID">Id of the component that was used.</param>
    /// <param name="usedOn">Object that component was used on.</param>
    /// <param name="shouldRun">Wheter CTRL was pressed.</param>
    /// <param name="info1">Extra info that client provides.</param>
    /// <param name="info2">Extra info that client provides.</param>
    /// <returns>
    /// Returns true if use was 'caught' and shouldn't be passed to other handlers.
    /// </returns>
    public delegate bool OnComponentUsedOnGameObject(int componentID, IGameObject usedOn, bool shouldRun, int info1, int info2);

    /// <summary>
    /// Delegates for use-on listeners.
    /// </summary>
    /// <param name="componentID">Id of the component that was used.</param>
    /// <param name="usedOn">GroundItem that component was used on.</param>
    /// <param name="shouldRun">Wheter CTRL was pressed.</param>
    /// <param name="info1">Extra info that client provides.</param>
    /// <param name="info2">Extra info that client provides.</param>
    /// <returns>
    /// Returns true if use was 'caught' and shouldn't be passed to other handlers.
    /// </returns>
    public delegate bool OnComponentUsedOnGroundItem(int componentID, IGroundItem usedOn, bool shouldRun, int info1, int info2);

    /// <summary>
    /// Delegates for use-on listeners.
    /// </summary>
    /// <param name="componentID">Id of the component that was used.</param>
    /// <param name="info1">Extra info that client provides.</param>
    /// <param name="info2">Extra info that client provides.</param>
    /// <param name="info3">The info3.</param>
    /// <param name="info4">The info4.</param>
    /// <returns>
    /// Returns true if use was 'caught' and shouldn't be passed to other handlers.
    /// </returns>
    public delegate bool OnComponentUsedOnComponent(int componentID, int info1, int info2, int info3, int info4);

    /// <summary>
    /// Happens when dialogue option is clicked.
    /// etg the click to continue clickable text.
    /// Returns false if we shouldn't progress to the next stage.
    /// </summary>
    public delegate bool OnDialogueClick(int stage, int extraData1);

    /// <summary>
    /// Delegate for string input listeners.
    /// </summary>
    /// <param name="str">String that was inputed.</param>
    public delegate void OnStringInput(string str);

    /// <summary>
    /// Delegate for int input listeners.
    /// </summary>
    /// <param name="value">Integer that was inputed.</param>
    public delegate void OnIntInput(int value);
}
