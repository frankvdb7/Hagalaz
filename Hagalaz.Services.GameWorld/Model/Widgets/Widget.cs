using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Messages.Protocol;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Model.Widgets
{
    /// <summary>
    /// Represend's interface.
    /// </summary>
    public class Widget : IWidget
    {
        /// <summary>
        /// Contains interface Script.
        /// </summary>
        /// <value>The script.</value>
        private readonly Dictionary<int, IWidget> _children = new();

        /// <summary>
        /// Contains handlers for component clicking.
        /// </summary>
        private readonly Dictionary<int, List<OnComponentClick>> _componentClickHandlers = new();

        /// <summary>
        /// Contains handlers for component dragging.
        /// </summary>
        private readonly Dictionary<int, List<OnComponentDragged>> _componentDragHandlers = new();

        /// <summary>
        /// Contains handlers for using on creatures.
        /// </summary>
        private readonly Dictionary<int, List<OnComponentUsedOnCreature>> _useOnCreatureHandlers = new();

        /// <summary>
        /// Contains handlers for using on gameobjects.
        /// </summary>
        private readonly Dictionary<int, List<OnComponentUsedOnGameObject>> _useOnGameObjectHandlers = new();

        /// <summary>
        /// Contains handlers for using on grounditems.
        /// </summary>
        private readonly Dictionary<int, List<OnComponentUsedOnGroundItem>> _useOnGroundItemHandlers = new();

        /// <summary>
        /// Contains handlers for using on components.
        /// </summary>
        private readonly Dictionary<int, List<OnComponentUsedOnComponent>> _useOnComponentHandlers = new();

        /// <summary>
        /// Contains owner of this interface.
        /// </summary>
        /// <value>The owner.</value>
        public ICharacter Owner { get; }

        /// <summary>
        /// Contains interface id.
        /// </summary>
        /// <value>The Id.</value>
        public int Id { get; set; }

        /// <summary>
        /// Contains interface Script.
        /// </summary>
        /// <value>The script.</value>
        public IWidgetScript Script { get; }

        /// <summary>
        /// Contains interface parentID.
        /// </summary>
        /// <value>The parent.</value>
        public int ParentId { get; }

        /// <summary>
        /// Contains interface parentSlot.
        /// </summary>
        /// <value>The parent slot.</value>
        public int ParentSlot { get; }

        /// <summary>
        /// Contains interface ShowID;
        /// </summary>
        /// <value>The transparency.</value>
        public int Transparency { get; }

        /// <summary>
        /// Get's if interface is frame.
        /// </summary>
        /// <value><c>true</c> if this instance is frame; otherwise, <c>false</c>.</value>
        public bool IsFrame { get; }

        /// <summary>
        /// Contains boolean if this interface is opened.
        /// </summary>
        /// <value><c>true</c> if opened; otherwise, <c>false</c>.</value>
        public bool IsOpened { get; private set; }

        /// <summary>
        /// Constructs new frame interface.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="id">Interface Id.</param>
        /// <param name="transparency">Interface transparency.</param>
        /// <param name="script">Script of interface.</param>
        public Widget(ICharacter owner, int id, int transparency, IWidgetScript script)
        {
            Owner = owner;
            Id = id;
            Transparency = transparency;
            IsFrame = true;
            Script = script;
        }

        /// <summary>
        /// Constructs new interface with given Id and scriptname.
        /// </summary>
        /// <param name="owner">Owner of this interface.</param>
        /// <param name="id">Interface Id.</param>
        /// <param name="parentId">Parent Interface id that will hold this interface.</param>
        /// <param name="parentSlot">Slot in parent interface where this interface will be holded.</param>
        /// <param name="transparency">Transparency of this interface.</param>
        /// <param name="script">The script.</param>
        public Widget(ICharacter owner, int id, int parentId, int parentSlot, int transparency, IWidgetScript script)
        {
            Owner = owner;
            Id = id;
            ParentId = parentId;
            ParentSlot = parentSlot;
            Transparency = transparency;
            IsFrame = false;
            Script = script;
        }

        /// <summary>
        /// Constructs new interface with given Id and scriptname.
        /// </summary>
        /// <param name="owner">Owner of this interface.</param>
        /// <param name="id">Interface Id.</param>
        /// <param name="parentId">Parent Id Interface that will hold this interface.</param>
        /// <param name="parentSlot">Slot in parent interface where this interface will be holded.</param>
        /// <param name="transparency">Transparency of this interface.</param>
        /// <param name="script">The script.</param>
        /// <param name="source">The source.</param>
        public Widget(ICharacter owner, int id, int parentId, int parentSlot, int transparency, IDialogueScript script, IRuneObject? source)
        {
            Owner = owner;
            Id = id;
            ParentId = parentId;
            ParentSlot = parentSlot;
            Transparency = transparency;
            IsFrame = false;
            Script = script;
        }

        /// <summary>
        /// Closes this interface.
        /// </summary>
        public void Close() => Owner.Widgets.CloseWidget(this);

        /// <summary>
        /// Attaches child interface to given slot.
        /// </summary>
        /// <param name="children">Child Interface that is being attached.</param>
        /// <param name="slot">Slot to which interface should be attached.</param>
        public void AttachChildren(IWidget children, int slot)
        {
            if (_children.TryAdd(slot, children))
            {
                return;
            }

            _children.Remove(slot);
            _children.Add(slot, children);
        }

        /// <summary>
        /// Detaches child from given slot.
        /// </summary>
        /// <param name="slot">Slot from which this interface should be detached.</param>
        public void DetachChildren(int slot) => _children.Remove(slot);

        /// <summary>
        /// Get's all childrens that are attached to this interface.
        /// </summary>
        /// <returns>List{Interface}.</returns>
        public IReadOnlyList<IWidget> GetAllChildren() => _children.Values.ToList();

        /// <summary>
        /// Get's if slot is used.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public bool SlotUsed(int slot) => _children.ContainsKey(slot);

        /// <summary>
        /// Get's child from given slot.
        /// </summary>
        /// <param name="slot">Slot from which children should be returned.</param>
        /// <returns>Interface.</returns>
        public IWidget? GetChild(int slot) => !SlotUsed(slot) ? null : _children[slot];

        /// <summary>
        /// Determines whether this instance can interrupt.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can interrupt; otherwise, <c>false</c>.
        /// </returns>
        public bool CanInterrupt() => Script.CanInterrupt();

        /// <summary>
        /// Called when [display change].
        /// </summary>
        public void OnDisplayChanged() => Script.OnDisplayChanged();

        /// <summary>
        /// Happens when interface is opened.
        /// </summary>
        public void OnOpen()
        {
            IsOpened = true;
            Script.Initialize(this);
            Script.OnOpen();
            Script.OnOpened();
        }

        /// <summary>
        /// Happens when interface is closed.
        /// </summary>
        public void OnClose()
        {
            Script.OnClose();
            Script.OnClosed();
            IsOpened = false;
        }

        /// <summary>
        /// Called when [component click].
        /// </summary>
        /// <param name="childID">The child Id.</param>
        /// <param name="clickType">Type of the click.</param>
        /// <param name="extraInfo1">The extra info1.</param>
        /// <param name="extraInfo2">The extra info2.</param>
        public void OnComponentClick(int childID, ComponentClickType clickType, int extraInfo1, int extraInfo2)
        {
            if (!_componentClickHandlers.TryGetValue(childID, out var handler))
            {
                if (Owner.Permissions.HasAtLeastXPermission(Permission.SystemAdministrator))
                {
                    Owner.SendChatMessage("click[id=" + Id + ",component_id=" + childID + ",extra1=" + extraInfo1 + ",extra2=" + extraInfo2 + ",type=" + clickType + "]", ChatMessageType.ConsoleText);
                }

                return;
            }

            foreach (var h in handler)
            {
                if (h.Invoke(childID, clickType, (short)extraInfo1, (short)extraInfo2))
                    return;
            }
        }

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
        public void OnComponentDrag(int childID, int dragedFromExtraInfo1, int dragedFromExtraInfo2, IWidget dragedToInterface, int dragedToChildID, int dragedToExtraInfo1, int dragedToExtraInfo2)
        {
            if (!_componentDragHandlers.TryGetValue(childID, out var handler))
            {
                if (Owner.Permissions.HasAtLeastXPermission(Permission.SystemAdministrator))
                {
                    Owner.SendChatMessage("on_component_drag[id=" + Id + ",component_id=" + childID + "]", ChatMessageType.ConsoleText);
                }

                return;
            }

            foreach (var h in handler)
            {
                if (h.Invoke(childID, dragedFromExtraInfo1, dragedFromExtraInfo2, dragedToInterface, dragedToChildID, dragedToExtraInfo1, dragedToExtraInfo2))
                    return;
            }
        }

        /// <summary>
        /// Happens when component is used on character.
        /// </summary>
        /// <param name="childID">childID which was used.</param>
        /// <param name="usedOn">Creature to which this component was used on.</param>
        /// <param name="shouldRun">Wheter CTRL was pressed.</param>
        /// <param name="extraInfo1">Extra info client provides.</param>
        /// <param name="extraInfo2">Extra info client provides.</param>
        public void OnComponentUsedOnCreature(int childID, ICreature usedOn, bool shouldRun, int extraInfo1, int extraInfo2)
        {
            if (!_useOnCreatureHandlers.TryGetValue(childID, out var handler))
            {
                if (Owner.Permissions.HasAtLeastXPermission(Permission.SystemAdministrator))
                    Owner.SendChatMessage("on_component_used_on_creature[id=" + Id + ",component_id=" + childID + "]", ChatMessageType.ConsoleText);
                return;
            }

            foreach (var h in handler)
            {
                if (h.Invoke(childID, usedOn, shouldRun, extraInfo1, extraInfo2))
                    return;
            }
        }

        /// <summary>
        /// Happens when component is used on character.
        /// </summary>
        /// <param name="childID">childID which was used.</param>
        /// <param name="usedOn">GameObject to which this component was used on.</param>
        /// <param name="shouldRun">Wheter CTRL was pressed.</param>
        /// <param name="extraInfo1">Extra info client provides.</param>
        /// <param name="extraInfo2">Extra info client provides.</param>
        public void OnComponentUsedOnGameObject(int childID, IGameObject usedOn, bool shouldRun, int extraInfo1, int extraInfo2)
        {
            if (!_useOnGameObjectHandlers.TryGetValue(childID, out var handler))
            {
                if (Owner.Permissions.HasAtLeastXPermission(Permission.SystemAdministrator))
                    Owner.SendChatMessage("on_component_used_on_object[id=" + Id + ",component_id=" + childID + "]", ChatMessageType.ConsoleText);
                return;
            }

            foreach (var h in handler)
            {
                if (h.Invoke(childID, usedOn, shouldRun, extraInfo1, extraInfo2))
                    return;
            }
        }

        /// <summary>
        /// Happens when component is used on character.
        /// </summary>
        /// <param name="childID">childID which was used.</param>
        /// <param name="usedOn">GameObject to which this component was used on.</param>
        /// <param name="shouldRun">Wheter CTRL was pressed.</param>
        /// <param name="extraInfo1">Extra info client provides.</param>
        /// <param name="extraInfo2">Extra info client provides.</param>
        public void OnComponentUsedOnGroundItem(int childID, IGroundItem usedOn, bool shouldRun, int extraInfo1, int extraInfo2)
        {
            if (!_useOnGroundItemHandlers.TryGetValue(childID, out var handler))
            {
                if (Owner.Permissions.HasAtLeastXPermission(Permission.SystemAdministrator))
                {
                    Owner.SendChatMessage("on_component_used_on_ground_item[id=" + Id + ",component_id=" + childID + "]", ChatMessageType.ConsoleText);
                }

                return;
            }

            foreach (var h in handler)
            {
                if (h.Invoke(childID, usedOn, shouldRun, extraInfo1, extraInfo2))
                    return;
            }
        }

        /// <summary>
        /// Happens when component is used on component.
        /// </summary>
        /// <param name="childID">childID which was used.</param>
        /// <param name="extraInfo1">Extra info client provides.</param>
        /// <param name="extraInfo2">Extra info client provides.</param>
        /// <param name="extraInfo3">The extra info3.</param>
        /// <param name="extraInfo4">The extra info4.</param>
        /// <returns></returns>
        public bool OnComponentUsedOnComponent(int childID, int extraInfo1, int extraInfo2, int extraInfo3, int extraInfo4)
        {
            if (!_useOnComponentHandlers.TryGetValue(childID, out var handler))
                return false;
            foreach (var h in handler)
            {
                if (h.Invoke(childID, extraInfo1, extraInfo2, extraInfo3, extraInfo4))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Draw's string to interface child.
        /// </summary>
        /// <param name="childID">Id of the child to draw string at.</param>
        /// <param name="str">String which should be drawed.</param>
        public void DrawString(int childID, string str) => Owner.Session.SendMessage(new DrawStringComponentMessage
        {
            ComponentId = Id,
            ChildId = childID,
            Value = str
        });

        /// <summary>
        /// Set's interface options.
        /// </summary>
        /// <param name="childID">Component Id of the interface to set options to.</param>
        /// <param name="min">Beginning array length.</param>
        /// <param name="max">Ending array length.</param>
        /// <param name="value">Value to set.</param>
        public void SetOptions(int childID, int min, int max, int value) => Owner.Session.SendMessage(new SetComponentOptionsMessage
        {
            Id = Id,
            ChildId = childID,
            MinimumSlot = min,
            MaximumSlot = max,
            Value = value
        });

        /// <summary>
        /// Set's child visibility.
        /// </summary>
        /// <param name="childID">Id of the child to set visibility.</param>
        /// <param name="visible">Wheter child should be invisible.</param>
        public void SetVisible(int childID, bool visible) => Owner.Session.SendMessage(new SetComponentVisibilityMessage
        {
            ComponentId = Id,
            ChildId = childID,
            Visible = visible
        });

        /// <summary>
        /// Sets the animation.
        /// </summary>
        /// <param name="childID">The child Id.</param>
        /// <param name="animationID">The animation Id.</param>
        public void SetAnimation(int childID, int animationID) => Owner.Session.SendMessage(new SetComponentAnimationMessage
        {
            ComponentId = Id,
            ChildId = childID,
            AnimationId = animationID
        });

        /// <summary>
        /// Draws the model at given child.
        /// </summary>
        /// <param name="childID">The child Id.</param>
        /// <param name="modelID">The model Id.</param>
        public void DrawModel(int childID, int modelID) => Owner.Session.SendMessage(new DrawModelComponentMessage
        {
            ComponentId = Id,
            ChildId = childID,
            ModelId = modelID
        });

        /// <summary>
        /// Draw's sprite at given child.
        /// </summary>
        /// <param name="childID">Id of the child where sprite should be drawed.</param>
        /// <param name="spriteID">Id of the sprite to be drawed.</param>
        public void DrawSprite(int childID, int spriteID) => Owner.Session.SendMessage(new DrawSpriteComponentMessage
        {
            ComponentId = Id,
            ChildId = childID,
            SpriteId = spriteID
        });

        /// <summary>
        /// Draw's character that will be viewing the interface head at the given child.
        /// </summary>
        /// <param name="childID">Id of the child where head should be drawed.</param>
        public void DrawCharacterHead(int childID) => Owner.Session.SendMessage(new DrawCharacterComponentMessage
        {
            ComponentId = Id,
            ChildId = childID,
            DrawSelf = true
        });

        /// <summary>
        /// Draws the character head.
        /// </summary>
        /// <param name="childID">The child identifier.</param>
        /// <param name="character">The character.</param>
        public void DrawCharacterHead(int childID, ICharacter character) => Owner.Session.SendMessage(new DrawCharacterComponentMessage
        {
            ComponentId = Id,
            ChildId = childID,
            CharacterName = character.DisplayName.StringToInt(),
            CharacterIndex = character.Index,
            DrawSelf = false
        });

        /// <summary>
        /// Draw's NPC head at given child.
        /// </summary>
        /// <param name="childID">Id of the child where npc head should be drawed.</param>
        /// <param name="npcID">Id of the npc which head should be drawed.</param>
        public void DrawNpcHead(int childID, int npcID) => Owner.Session.SendMessage(new DrawNpcComponentMessage
        {
            NpcId = npcID,
            ChildId = childID,
            ComponentId = Id
        });

        /// <summary>
        /// Draws the item.
        /// </summary>
        /// <param name="childID">The child Id.</param>
        /// <param name="itemID">The item Id.</param>
        /// <param name="count">The amount.</param>
        public void DrawItem(int childID, int itemID, int count) => Owner.Session.SendMessage(new DrawItemComponentMessage
        {
            ComponentId = Id,
            ChildId = childID,
            ItemId = itemID,
            ItemCount = count
        });

        /// <summary>
        /// Attaches component click handler to given childID.
        /// </summary>
        /// <param name="childID">childID to which should be listened to.</param>
        /// <param name="handler">Handler which will be invoked when there's click on given childID.</param>
        public void AttachClickHandler(int childID, OnComponentClick handler)
        {
            if (_componentClickHandlers.TryGetValue(childID, out var clickHandler))
            {
                clickHandler.Add(handler);
            }
            else
            {
                var hList = new List<OnComponentClick>
                {
                    handler
                };
                _componentClickHandlers.Add(childID, hList);
            }
        }

        /// <summary>
        /// Detaches given handler from given childID.
        /// </summary>
        /// <param name="childID">childID from which handler should be detached.</param>
        /// <param name="handler">handler which should be detached.</param>
        public void DetachClickHandler(int childID, OnComponentClick handler)
        {
            if (!_componentClickHandlers.TryGetValue(childID, out var clickHandler))
                return;
            clickHandler.Remove(handler);
        }

        /// <summary>
        /// Attaches component drag handler to given childID.
        /// </summary>
        /// <param name="childID">childID to which should be listened to.</param>
        /// <param name="handler">Handler which will be invoked when there's drag from given childID.</param>
        public void AttachDragHandler(int childID, OnComponentDragged handler)
        {
            if (_componentDragHandlers.TryGetValue(childID, out var dragHandler))
                dragHandler.Add(handler);
            else
            {
                var hList = new List<OnComponentDragged>
                {
                    handler
                };
                _componentDragHandlers.Add(childID, hList);
            }
        }

        /// <summary>
        /// Detaches given drag handler from given childID.
        /// </summary>
        /// <param name="childID">childID from which handler should be detached.</param>
        /// <param name="handler">handler which should be detached.</param>
        public void DetachDragHandler(int childID, OnComponentDragged handler)
        {
            if (!_componentDragHandlers.TryGetValue(childID, out var dragHandler))
                return;
            dragHandler.Remove(handler);
        }

        /// <summary>
        /// Attaches useOnCreature handler to given childID.
        /// </summary>
        /// <param name="childID">childID to which should be listened to.</param>
        /// <param name="handler">Handler which will be invoked when there's useoncharacter from given childID.</param>
        public void AttachUseOnCreatureHandler(int childID, OnComponentUsedOnCreature handler)
        {
            if (_useOnCreatureHandlers.TryGetValue(childID, out var creatureHandler))
                creatureHandler.Add(handler);
            else
            {
                var hList = new List<OnComponentUsedOnCreature>
                {
                    handler
                };
                _useOnCreatureHandlers.Add(childID, hList);
            }
        }

        /// <summary>
        /// Detaches given useOnCreature handler from given childID.
        /// </summary>
        /// <param name="childID">childID from which handler should be detached.</param>
        /// <param name="handler">handler which should be detached.</param>
        public void DetachUseOnCreatureHandler(int childID, OnComponentUsedOnCreature handler)
        {
            if (!_useOnCreatureHandlers.TryGetValue(childID, out var creatureHandler))
                return;
            creatureHandler.Remove(handler);
        }

        /// <summary>
        /// Attaches useOnObject handler to given childID.
        /// </summary>
        /// <param name="childID">childID to which should be listened to.</param>
        /// <param name="handler">Handler which will be invoked when there's useonobject from given childID.</param>
        public void AttachUseOnObjectHandler(int childID, OnComponentUsedOnGameObject handler)
        {
            if (_useOnGameObjectHandlers.TryGetValue(childID, out var objectHandler))
                objectHandler.Add(handler);
            else
            {
                var hList = new List<OnComponentUsedOnGameObject>
                {
                    handler
                };
                _useOnGameObjectHandlers.Add(childID, hList);
            }
        }

        /// <summary>
        /// Detaches given useOnObject handler from given childID.
        /// </summary>
        /// <param name="childID">childID from which handler should be detached.</param>
        /// <param name="handler">handler which should be detached.</param>
        public void DetachUseOnObjectHandler(int childID, OnComponentUsedOnGameObject handler)
        {
            if (!_useOnGameObjectHandlers.TryGetValue(childID, out var objectHandler))
                return;
            objectHandler.Remove(handler);
        }

        /// <summary>
        /// Attaches useOnGroundItem handler to given childID.
        /// </summary>
        /// <param name="childID">childID to which should be listened to.</param>
        /// <param name="handler">Handler which will be invoked when there's useonobject from given childID.</param>
        public void AttachUseOnGroundItemHandler(int childID, OnComponentUsedOnGroundItem handler)
        {
            if (_useOnGroundItemHandlers.TryGetValue(childID, out var itemHandler))
                itemHandler.Add(handler);
            else
            {
                var hList = new List<OnComponentUsedOnGroundItem>
                {
                    handler
                };
                _useOnGroundItemHandlers.Add(childID, hList);
            }
        }

        /// <summary>
        /// Detaches given useOnGroundItem handler from given childID.
        /// </summary>
        /// <param name="childID">childID from which handler should be detached.</param>
        /// <param name="handler">handler which should be detached.</param>
        public void DetachUseOnGroundItemHandler(int childID, OnComponentUsedOnGroundItem handler)
        {
            if (!_useOnGroundItemHandlers.TryGetValue(childID, out var itemHandler))
                return;
            itemHandler.Remove(handler);
        }

        /// <summary>
        /// Attaches useOnComponent handler to given childID.
        /// </summary>
        /// <param name="childID">childID to which should be listened to.</param>
        /// <param name="handler">Handler which will be invoked when there's useoncomponent from given childID.</param>
        public void AttachUseOnComponentHandler(int childID, OnComponentUsedOnComponent handler)
        {
            if (_useOnComponentHandlers.TryGetValue(childID, out var componentHandler))
                componentHandler.Add(handler);
            else
            {
                var hList = new List<OnComponentUsedOnComponent>
                {
                    handler
                };
                _useOnComponentHandlers.Add(childID, hList);
            }
        }

        /// <summary>
        /// Detaches given useOnComponent handler from given childID.
        /// </summary>
        /// <param name="childID">childID from which handler should be detached.</param>
        /// <param name="handler">handler which should be detached.</param>
        public void DetachUseOnComponentHandler(int childID, OnComponentUsedOnComponent handler)
        {
            if (!_useOnComponentHandlers.TryGetValue(childID, out var componentHandler))
                return;
            componentHandler.Remove(handler);
        }

        /// <summary>
        /// Detache's all click handlers.
        /// </summary>
        public void DetachClickHandlers() => _componentClickHandlers.Clear();

        /// <summary>
        /// Detache's all click handlers on the given child id.
        /// </summary>
        /// <param name="childId">The child identifier.</param>
        /// <returns>True if successfully removed all clickhandlers on the given child id.</returns>
        public bool DetachClickHandlers(int childId) => _componentClickHandlers.Remove(childId);

        /// <summary>
        /// Detache's all drag handlers.
        /// </summary>
        public void DetachDragHandlers() => _componentDragHandlers.Clear();

        /// <summary>
        /// Detache's all use on creature handlers.
        /// </summary>
        public void DetachUseOnCreatureHandlers() => _useOnCreatureHandlers.Clear();

        /// <summary>
        /// Detache's all use on object handlers.
        /// </summary>
        public void DetachUseOnObjectHandlers() => _useOnGameObjectHandlers.Clear();

        /// <summary>
        /// Detache's all use on ground item handlers.
        /// </summary>
        public void DetachUseOnGroundItemHandlers() => _useOnGroundItemHandlers.Clear();

        /// <summary>
        /// Detaches the use on component handlers.
        /// </summary>
        public void DetachUseOnComponentHandlers() => _useOnComponentHandlers.Clear();
    }
}