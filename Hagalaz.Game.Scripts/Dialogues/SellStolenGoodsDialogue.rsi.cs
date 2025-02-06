using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Configuration;
using Hagalaz.Game.Model.Items;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Dialogues
{
    /// <summary>
    /// </summary>
    public class SellStolenGoodsDialogue : NpcDialogueScript
    {
        private readonly IRatesService _ratesService;

        /// <summary>
        ///     The goods ids
        /// </summary>
        private static readonly int[] _goodsIds = [9028, 9029, 9030, 9031, 9032, 9033, 9034, 9035, 9036, 9037, 9038, 9039, 9040, 9041, 9042, 9043];

        public SellStolenGoodsDialogue(ICharacterContextAccessor characterContextAccessor, IRatesService ratesService) : base(characterContextAccessor)
        {
            _ratesService = ratesService;
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            AttachDialogueContinueClickHandler(0, (extraData1, extraData2) =>
            {
                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "Greetings adventurer!", "Do you want me to fence any stolen goods?");
                return true;
            });
            AttachDialogueContinueClickHandler(1, (extraData1, extraData2) =>
            {
                DefaultOptionDialogue("Yes, please!", "No, thanks!");
                return true;
            });
            AttachDialogueOptionClickHandler("Yes, please!", (extraData1, extraData2) =>
            {
                var rateValue = _ratesService.GetRate<ItemOptions>(i => i.CoinCountRate);
                foreach (var t in _goodsIds)
                {
                    var good = new Item(t, int.MaxValue);
                    var removed = Owner.Inventory.Remove(good);
                    if (removed <= 0)
                    {
                        continue;
                    }

                    var price = (ulong)(removed * good.ItemDefinition.TradeValue * rateValue);
                    if (price > int.MaxValue)
                    {
                        price = int.MaxValue;
                    }

                    Owner.MoneyPouch.Add((int)price);
                }

                StandardNpcDialogue(TalkingTo, DialogueAnimations.CalmTalk, "There you go!", "Now on your way, " + Owner.DisplayName + "!", "Watch out for the guards!");
                return false;
            });
            AttachDialogueContinueClickHandler(2, (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return true;
            });

            AttachDialogueOptionClickHandler("No, thanks!", (extraData1, extraData2) =>
            {
                Owner.Widgets.CloseChatboxOverlay();
                return false;
            });
        }
    }
}