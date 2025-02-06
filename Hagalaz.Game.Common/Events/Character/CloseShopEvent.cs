using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// 
    /// </summary>
    public class CloseShopEvent : CharacterEvent
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        public CloseShopEvent(ICharacter target) : base(target)
        {
        }
    }
}