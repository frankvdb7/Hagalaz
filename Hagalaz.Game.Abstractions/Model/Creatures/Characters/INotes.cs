namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// Defines the contract for a single in-game note, containing its content, ID, and color.
    /// </summary>
    public interface INote
    {
        /// <summary>
        /// Gets the unique identifier for the note.
        /// </summary>
        int Id { get; }
        /// <summary>
        /// Gets the text content of the note.
        /// </summary>
        string Text { get; }
        /// <summary>
        /// Gets the color code of the note.
        /// </summary>
        int Color { get; }
    }
    /// <summary>
    /// Defines the contract for managing a character's collection of in-game notes.
    /// </summary>
    public interface INotes
    {
        /// <summary>
        /// Gets the note at the specified index in the collection.
        /// </summary>
        /// <param name="index">The zero-based index of the note to get.</param>
        /// <returns>The <see cref="INote"/> at the specified index, or <c>null</c> if the index is out of range.</returns>
        INote? this[int index] { get; }
        /// <summary>
        /// Gets the current number of notes in the collection.
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Gets the maximum number of notes that can be stored.
        /// </summary>
        int Capacity { get; }
        /// <summary>
        /// Adds a new note with the specified text.
        /// </summary>
        /// <param name="text">The text content of the new note.</param>
        void Add(string text);
        /// <summary>
        /// Deletes a note by its unique identifier.
        /// </summary>
        /// <param name="id">The ID of the note to delete.</param>
        void Delete(int id);
        /// <summary>
        /// Deletes all notes in the collection.
        /// </summary>
        void DeleteAll();
        /// <summary>
        /// Sets the color of a specific note.
        /// </summary>
        /// <param name="id">The ID of the note to modify.</param>
        /// <param name="color">The new color code to set.</param>
        void SetColor(int id, int color);
        /// <summary>
        /// Sets the text content of a specific note.
        /// </summary>
        /// <param name="id">The ID of the note to modify.</param>
        /// <param name="text">The new text content.</param>
        void SetText(int id, string text);
        /// <summary>
        /// Sends the complete list of notes to the client.
        /// </summary>
        void Refresh();
    }
}
