namespace Hagalaz.Game.Abstractions.Model.Creatures.Characters
{
    /// <summary>
    /// 
    /// </summary>
    public interface INote
    {
        /// <summary>
        /// Contains the identifier.
        /// </summary>
        int Id { get; }
        /// <summary>
        /// Contains the text.
        /// </summary>
        string Text { get; }
        /// <summary>
        /// Contains the colour.
        /// </summary>
        int Color { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface INotes
    {
        /// <summary>
        /// Gets the <see cref="INote"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="INote"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        INote? this[int index] { get; }
        /// <summary>
        /// Contains the count.
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Contains the capacity.
        /// </summary>
        int Capacity { get; }
        /// <summary>
        /// Adds the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        void Add(string text);
        /// <summary>
        /// Deletes the specified note.
        /// </summary>
        /// <param name="note">The note.</param>
        void Delete(int id);
        /// <summary>
        /// Deletes all.
        /// </summary>
        void DeleteAll();
        /// <summary>
        /// Sets the color of the note
        /// </summary>
        /// <param name="note">The note.</param>
        void SetColor(int id, int color);
        /// <summary>
        /// Sets the text of the note
        /// </summary>
        /// <param name="note">The note.</param>
        void SetText(int id, string text);
        /// <summary>
        /// Refreshes the notes.
        /// </summary>
        void Refresh();
    }
}
