namespace Hagalaz.Game.Abstractions.Services.Model
{
    /// <summary>
    /// Identifies an Agility course. The values match the persisted course definition ids.
    /// </summary>
    public enum AgilityCourseId : byte
    {
        /// <summary>
        /// The basic Gnome Stronghold course.
        /// </summary>
        GnomeBasic = 1,

        /// <summary>
        /// The advanced Gnome Stronghold course.
        /// </summary>
        GnomeAdvanced = 2
    }
}
