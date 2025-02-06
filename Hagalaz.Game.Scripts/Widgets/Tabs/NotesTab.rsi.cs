using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    /// </summary>
    public class NotesTab : WidgetScript
    {
        /// <summary>
        ///     The note handler.
        /// </summary>
        private OnStringInput? _noteHandler;

        /// <summary>
        ///     The current note index.
        /// </summary>
        private INote? _selectedNote;

        public NotesTab(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor) { }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.SetOptions(9, 0, (ushort)Owner.Notes.Capacity, 2621470);
            InterfaceInstance.SetVisible(44, true);
            InterfaceInstance.SetVisible(3, true);
            Owner.Configurations.SendStandardConfiguration(1437, 1);
            Owner.Configurations.SendStandardConfiguration(1439, -1);
            InterfaceInstance.AttachClickHandler(3, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    _noteHandler = Owner.Widgets.StringInputHandler = value =>
                    {
                        _noteHandler = Owner.Widgets.StringInputHandler = null;
                        if (value == string.Empty)
                        {
                            Owner.SendChatMessage("A note can't be empty.");
                            return;
                        }

                        Owner.Notes.Add(value);
                    };
                    Owner.Configurations.SendTextInput("Add note:");
                    return true;
                }

                return false;
            });

            OnComponentClick deleteClickHandler = (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick && _selectedNote != null)
                {
                    Owner.Notes.Delete(_selectedNote.Id);
                    return true;
                }

                if (clickType == ComponentClickType.Option2Click)
                {
                    Owner.Notes.DeleteAll();
                    return true;
                }

                return false;
            };

            InterfaceInstance.AttachClickHandler(8, deleteClickHandler);
            InterfaceInstance.AttachClickHandler(11, deleteClickHandler);

            InterfaceInstance.AttachClickHandler(9, (componentID, clickType, extraData1, extraData2) =>
            {
                var noteId = extraData2;
                if (noteId < 0 || noteId >= Owner.Notes.Count)
                {
                    return false;
                }

                var note = Owner.Notes[noteId];
                if (note == null)
                {
                    return false;
                }

                if (clickType == ComponentClickType.LeftClick)
                {
                    return true;
                }

                if (clickType == ComponentClickType.Option2Click)
                {
                    SetSelectedNote(note);
                    _noteHandler = Owner.Widgets.StringInputHandler = value =>
                    {
                        _noteHandler = Owner.Widgets.StringInputHandler = null;
                        if (value == string.Empty)
                        {
                            Owner.SendChatMessage("A note can't be empty.");
                            return;
                        }

                        Owner.Notes.SetText(noteId, value);
                    };
                    Owner.Configurations.SendTextInput("Edit note:");
                    return true;
                }

                if (clickType == ComponentClickType.Option3Click)
                {
                    SetSelectedNote(note);
                    InterfaceInstance.SetVisible(16, true);
                    return true;
                }

                if (clickType == ComponentClickType.Option4Click)
                {
                    Owner.Notes.Delete(note.Id);
                    return true;
                }

                return false;
            });

            OnComponentClick colourClickHandler = (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType == ComponentClickType.LeftClick)
                {
                    Owner.Notes.SetColor(_selectedNote.Id, (componentID - 35) / 2);
                    return true;
                }

                return false;
            };
            InterfaceInstance.AttachClickHandler(35, colourClickHandler);
            InterfaceInstance.AttachClickHandler(37, colourClickHandler);
            InterfaceInstance.AttachClickHandler(39, colourClickHandler);
            InterfaceInstance.AttachClickHandler(41, colourClickHandler);

            Owner.Notes.Refresh();
        }

        /// <summary>
        ///     Sets the current note.
        /// </summary>
        /// <param name="note">The note.</param>
        public void SetSelectedNote(INote note)
        {
            _selectedNote = note;
            Owner.Configurations.SendStandardConfiguration(1439, note.Id);
        }

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose()
        {
            if (_noteHandler != null)
            {
                if (_noteHandler == Owner.Widgets.StringInputHandler)
                {
                    Owner.Widgets.StringInputHandler = null;
                }

                _noteHandler = null;
            }
        }
    }
}