using System.Collections.Generic;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Abstractions.Model.Widgets
{
    /// <summary>
    /// 
    /// </summary>
    public interface IWidget
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        int Id { get; set; }
        /// <summary>
        /// Gets the owner.
        /// </summary>
        /// <value>
        /// The owner.
        /// </value>
        ICharacter Owner { get; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="IWidget"/> is opened.
        /// </summary>
        /// <value>
        ///   <c>true</c> if opened; otherwise, <c>false</c>.
        /// </value>
        bool IsOpened { get; }
        /// <summary>
        /// Get's if interface is frame.
        /// </summary>
        /// <value><c>true</c> if this instance is frame; otherwise, <c>false</c>.</value>
        bool IsFrame { get; }
        /// <summary>
        /// Contains interface parentID.
        /// </summary>
        /// <value>The parent.</value>
        int ParentId { get; }
        /// <summary>
        /// Gets the script.
        /// </summary>
        /// <value>
        /// The script.
        /// </value>
        IWidgetScript Script { get; }
        /// <summary>
        /// Contains interface parentSlot.
        /// </summary>
        /// <value>The parent slot.</value>
        int ParentSlot { get; }
        /// <summary>
        /// Gets the transparency.
        /// </summary>
        /// <value>
        /// The transparency.
        /// </value>
        int Transparency { get; }
        /// <summary>
        /// Attaches the children.
        /// </summary>
        /// <param name="children">The children.</param>
        /// <param name="slot">The slot.</param>
        void AttachChildren(IWidget children, int slot);
        /// <summary>
        /// Attaches component click handler to given childID.
        /// </summary>
        /// <param name="childID">childID to which should be listened to.</param>
        /// <param name="handler">Handler which will be invoked when there's click on given childID.</param>
        void AttachClickHandler(int childID, OnComponentClick handler);
        /// <summary>
        /// Attaches component drag handler to given childID.
        /// </summary>
        /// <param name="childID">childID to which should be listened to.</param>
        /// <param name="handler">Handler which will be invoked when there's drag from given childID.</param>
        void AttachDragHandler(int childID, OnComponentDragged handler);
        /// <summary>
        /// Attaches useOnObject handler to given childID.
        /// </summary>
        /// <param name="childID">childID to which should be listened to.</param>
        /// <param name="handler">Handler which will be invoked when there's useonobject from given childID.</param>
        void AttachUseOnObjectHandler(int childID, OnComponentUsedOnGameObject handler);
        /// <summary>
        /// Attaches useOnGroundItem handler to given childID.
        /// </summary>
        /// <param name="childID">childID to which should be listened to.</param>
        /// <param name="handler">Handler which will be invoked when there's useonobject from given childID.</param>
        void AttachUseOnGroundItemHandler(int childID, OnComponentUsedOnGroundItem handler);
        /// <summary>
        /// Attaches useOnComponent handler to given childID.
        /// </summary>
        /// <param name="childID">childID to which should be listened to.</param>
        /// <param name="handler">Handler which will be invoked when there's useoncomponent from given childID.</param>
        void AttachUseOnComponentHandler(int childID, OnComponentUsedOnComponent handler);
        /// <summary>
        /// Attaches useOnCreature handler to given childID.
        /// </summary>
        /// <param name="childID">childID to which should be listened to.</param>
        /// <param name="handler">Handler which will be invoked when there's useoncharacter from given childID.</param>
        void AttachUseOnCreatureHandler(int childID, OnComponentUsedOnCreature handler);
        /// <summary>
        /// Detaches the children.
        /// </summary>
        /// <param name="slot">The slot.</param>
        void DetachChildren(int slot);
        /// <summary>
        /// Detaches given handler from given childID.
        /// </summary>
        /// <param name="childID">childID from which handler should be detached.</param>
        /// <param name="handler">handler which should be detached.</param>
        void DetachClickHandler(int childID, OnComponentClick handler);
        /// <summary>
        /// Detache's all click handlers on the given child id.
        /// </summary>
        /// <param name="childId">The child identifier.</param>
        /// <returns>True if successfully removed all clickhandlers on the given child id.</returns>
        bool DetachClickHandlers(int childId);
        /// <summary>
        /// Gets the child.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <returns></returns>
        IWidget? GetChild(int slot);
        /// <summary>
        /// Gets all childrens.
        /// </summary>
        /// <returns></returns>
        IReadOnlyList<IWidget> GetAllChildren();
        /// <summary>
        /// Determines whether this instance can interrupt.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can interrupt; otherwise, <c>false</c>.
        /// </returns>
        bool CanInterrupt();
        /// <summary>
        /// Closes this instance.
        /// </summary>
        void Close();
        /// <summary>
        /// Slots the used.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <returns></returns>
        bool SlotUsed(int slot);
        /// <summary>
        /// Called when [display change].
        /// </summary>
        void OnDisplayChanged();
        /// <summary>
        /// Called when [open].
        /// </summary>
        void OnOpen();
        /// <summary>
        /// Raises the Close event.
        /// </summary>
        void OnClose();

        /// <summary>
        /// Draws the item.
        /// </summary>
        /// <param name="childID">The child Id.</param>
        /// <param name="itemID">The item Id.</param>
        /// <param name="count">The amount.</param>
        void DrawItem(int childID, int itemID, int count);

        /// <summary>
        /// Draw's string to interface child.
        /// </summary>
        /// <param name="childID">Id of the child to draw string at.</param>
        /// <param name="str">String which should be drawed.</param>
        void DrawString(int childID, string str);

        /// <summary>
        /// Set's child visibility.
        /// </summary>
        /// <param name="childID">Id of the child to set visibility.</param>
        /// <param name="visible">Wheter child should be invisible.</param>
        void SetVisible(int childID, bool visible);

        /// <summary>
        /// Set's interface options.
        /// </summary>
        /// <param name="childID">Component Id of the interface to set options to.</param>
        /// <param name="min">Beginning array length.</param>
        /// <param name="max">Ending array length.</param>
        /// <param name="value">Value to set.</param>
        void SetOptions(int childID, int min, int max, int value);

        /// <summary>
        /// Sets the animation.
        /// </summary>
        /// <param name="childID">The child Id.</param>
        /// <param name="animationID">The animation Id.</param>
        void SetAnimation(int childID, int animationID);

        /// <summary>
        /// Draws the model at given child.
        /// </summary>
        /// <param name="childID">The child Id.</param>
        /// <param name="modelID">The model Id.</param>
        void DrawModel(int childID, int modelID);

        /// <summary>
        /// Draw's sprite at given child.
        /// </summary>
        /// <param name="childID">Id of the child where sprite should be drawed.</param>
        /// <param name="spriteID">Id of the sprite to be drawed.</param>
        void DrawSprite(int childID, int spriteID);

        /// <summary>
        /// Draw's character that will be viewing the interface head at the given child.
        /// </summary>
        /// <param name="childID">Id of the child where head should be drawed.</param>
        void DrawCharacterHead(int childID);

        /// <summary>
        /// Draws the character head.
        /// </summary>
        /// <param name="childID">The child identifier.</param>
        /// <param name="character">The character.</param>
        void DrawCharacterHead(int childID, ICharacter character);

        /// <summary>
        /// Draw's NPC head at given child.
        /// </summary>
        /// <param name="childID">Id of the child where npc head should be drawed.</param>
        /// <param name="npcID">Id of the npc which head should be drawed.</param>
        void DrawNpcHead(int childID, int npcID);
        /// <summary>
        /// Called when [component click].
        /// </summary>
        /// <param name="childID">The child Id.</param>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="extraInfo1">The extra info1.</param>
        /// <param name="extraInfo2">The extra info2.</param>
        void OnComponentClick(int childID, ComponentClickType clickType, int extraInfo1, int extraInfo2);
        /// <summary>
        /// Happens when component is used on character.
        /// </summary>
        /// <param name="childID">childID which was used.</param>
        /// <param name="usedOn">GameObject to which this component was used on.</param>
        /// <param name="shouldRun">Wheter CTRL was pressed.</param>
        /// <param name="extraInfo1">Extra info client provides.</param>
        /// <param name="extraInfo2">Extra info client provides.</param>
        void OnComponentUsedOnGroundItem(int childID, IGroundItem usedOn, bool shouldRun, int extraInfo1, int extraInfo2);
        /// <summary>
        /// Happens when component is used on character.
        /// </summary>
        /// <param name="childID">childID which was used.</param>
        /// <param name="usedOn">Creature to which this component was used on.</param>
        /// <param name="shouldRun">Wheter CTRL was pressed.</param>
        /// <param name="extraInfo1">Extra info client provides.</param>
        /// <param name="extraInfo2">Extra info client provides.</param>
        void OnComponentUsedOnCreature(int childID, ICreature usedOn, bool shouldRun, int extraInfo1, int extraInfo2);
        /// <summary>
        /// Happens when component is draged.
        /// </summary>
        /// <param name="childID">childID which was clicked.</param>
        /// <param name="dragedFromExtraInfo1">The draged from extra info1.</param>
        /// <param name="dragedFromExtraInfo2">The draged from extra info2.</param>
        /// <param name="dragedToInterface">Interface to which given child was draged.</param>
        /// <param name="dragedToChildID">Id of the child that given child was draged to.</param>
        /// <param name="dragedToExtraInfo1">The draged to extra info1.</param>
        /// <param name="dragedToExtraInfo2">The draged to extra info2.</param>
        void OnComponentDrag(int childID, int dragedFromExtraInfo1, int dragedFromExtraInfo2, IWidget dragedToInterface, int dragedToChildID, int dragedToExtraInfo1, int dragedToExtraInfo2);
        /// <summary>
        /// Happens when component is used on component.
        /// </summary>
        /// <param name="childID">childID which was used.</param>
        /// <param name="extraInfo1">Extra info client provides.</param>
        /// <param name="extraInfo2">Extra info client provides.</param>
        /// <param name="extraInfo3">The extra info3.</param>
        /// <param name="extraInfo4">The extra info4.</param>
        /// <returns></returns>
        bool OnComponentUsedOnComponent(int childID, int extraInfo1, int extraInfo2, int extraInfo3, int extraInfo4);
        /// <summary>
        /// Happens when component is used on character.
        /// </summary>
        /// <param name="childID">childID which was used.</param>
        /// <param name="usedOn">GameObject to which this component was used on.</param>
        /// <param name="shouldRun">Wheter CTRL was pressed.</param>
        /// <param name="extraInfo1">Extra info client provides.</param>
        /// <param name="extraInfo2">Extra info client provides.</param>
        void OnComponentUsedOnGameObject(int childID, IGameObject usedOn, bool shouldRun, int extraInfo1, int extraInfo2);
    }
}
