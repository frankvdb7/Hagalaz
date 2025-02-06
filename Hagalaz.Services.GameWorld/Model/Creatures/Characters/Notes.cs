using System;
using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public class Notes : INotes, IHydratable<HydratedNotesDto>, IDehydratable<HydratedNotesDto>
    {
        /// <summary>
        /// 
        /// </summary>
        public class Note : INote
        {
            /// <summary>
            /// Contains the identifier.
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Contains the text.
            /// </summary>
            public string Text { get; set; } = string.Empty;

            /// <summary>
            /// Contains the color.
            /// </summary>
            public int Color { get; set; }
        }

        /// <summary>
        /// The owner.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// The notes.
        /// </summary>
        private readonly List<Note> _notes = new List<Note>(30);

        /// <summary>
        /// Gets the <see cref="Note"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="Note"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public INote? this[int index]
        {
            get
            {
                if (index < 0 || index >= _notes.Count)
                    throw new IndexOutOfRangeException(nameof(index));
                return _notes[index];
            }
        }

        /// <summary>
        /// Contains the count.
        /// </summary>
        public int Count => _notes.Count;

        /// <summary>
        /// Contains the capacity.
        /// </summary>
        public int Capacity => _notes.Capacity;

        /// <summary>
        /// Initializes a new instance of the <see cref="Notes" /> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        public Notes(ICharacter owner) => _owner = owner;

        /// <summary>
        /// Adds the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        public void Add(string text)
        {
            if (text.Length > 50)
            {
                _owner.SendChatMessage("Notes can only be up to 50 characters long.");
                return;
            }

            var note = new Note
            {
                Text = text,
                Id = Count
            };
            _notes.Add(note);

            SetText(note.Id, note.Text);
            SetColor(note.Id, note.Color);
        }

        /// <summary>
        /// Deletes the specified note.
        /// </summary>
        /// <param name="note">The note.</param>
        public void Delete(int id)
        {
            var note = _notes.FirstOrDefault(n => n.Id == id);
            if (note == null)
            {
                return;
            }
            _notes.Remove(note);
            _owner.Configurations.SendGlobalCs2String(149 + note.Id, string.Empty);
            _owner.Configurations.SendStandardConfiguration(1439, -1);
        }

        /// <summary>
        /// Deletes all.
        /// </summary>
        public void DeleteAll()
        {
            foreach (var note in _notes)
            {
                _owner.Configurations.SendGlobalCs2String(149 + note.Id, string.Empty);
            }
            _owner.Configurations.SendStandardConfiguration(1439, -1);
            _notes.Clear();
        }

        /// <summary>
        /// Refreshes the note.
        /// </summary>
        /// <param name="note">The note.</param>
        public void SetText(int id, string text)
        {
            var note = _notes.FirstOrDefault(n => n.Id == id);
            if (note == null)
            {
                return;
            }
            note.Text = text;
            _owner.Configurations.SendGlobalCs2String(149 + id, text);
        }

        /// <summary>
        /// Refreshes the colour.
        /// </summary>
        /// <param name="note">The note.</param>
        public void SetColor(int id, int color)
        {
            var note = _notes.FirstOrDefault(n => n.Id == id);
            if (note == null)
            {
                return;
            }
            note.Color = color;
            _owner.Configurations.SendBitConfiguration(6316 + id, color);
        }

        /// <summary>
        /// Refreshes the notes.
        /// </summary>
        public void Refresh()
        {
            var colourConfig1 = 0;
            var colourConfig2 = 0;
            foreach (var note in _notes)
            {
                _owner.Configurations.SendGlobalCs2String(149 + note.Id, note.Text);
                if (note.Id < 16)
                    colourConfig1 += note.Color * (int)Math.Pow(4, note.Id);
                else
                    colourConfig2 += note.Color * (int)Math.Pow(4, note.Id);
            }

            _owner.Configurations.SendStandardConfiguration(1440, colourConfig1);
            _owner.Configurations.SendStandardConfiguration(1441, colourConfig2);
        }

        public void Hydrate(HydratedNotesDto hydration)
        {
            foreach (var note in hydration.Notes)
            {
                _notes.Add(new Note
                {
                    Id = note.Id,
                    Color = note.Color,
                    Text = note.Text
                });
            }
        }

        public HydratedNotesDto Dehydrate() => new()
        {
            Notes = _notes.Select(n => new HydratedNotesDto.NoteDto { Id = n.Id, Color = n.Color, Text = n.Text }).ToList()
        };
    }
}