using Hagalaz.Game.Abstractions.Model.Creatures.Characters;

namespace Hagalaz.Game.Common.Events.Character
{
    /// <summary>
    /// 
    /// </summary>
    public class OpenShopEvent : CharacterEvent
    {
        /// <summary>
        /// 
        /// </summary>
        public int ShopId { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="shopId"></param>
        public OpenShopEvent(ICharacter target, int shopId) : base(target)
        {
            ShopId = shopId;
        }
    }
}