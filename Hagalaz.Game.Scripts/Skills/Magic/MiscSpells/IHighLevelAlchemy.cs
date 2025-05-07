using Hagalaz.Game.Abstractions.Model.Items;

namespace Hagalaz.Game.Scripts.Skills.Magic.MiscSpells
{
    public interface IHighLevelAlchemy
    {
        /// <summary>
        ///     Casts the spell.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        bool Cast(IItem item);
    }
}